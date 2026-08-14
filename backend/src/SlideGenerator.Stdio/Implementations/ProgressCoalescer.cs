/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: ProgressCoalescer.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using StreamJsonRpc;

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     Forwards Request/Job/Row progress and log lines to the client as JSON-RPC notifications
///     (<c>progress/request</c>, <c>progress/jobs</c>, <c>progress/rows</c>, <c>log/entries</c>).
///     Only <c>Jobs</c> is a persisted, buffered table now — <see cref="IJobsRepository" /> (Generator)
///     itself owns the buffer/flush/DB-upsert cycle (see <c>BufferedRepository</c>); this class simply
///     subscribes to its <see cref="IJobsRepository.Flushed" /> event and relays each batch. Requests are
///     write-once (persisted directly by <c>Service.CreateAsync</c>, not through here) and Rows are never
///     persisted at all — both are forwarded to the client immediately, un-buffered. Logs remain
///     append-only buffered here (never coalesced, unlike Progress) since every line matters.
///     The connection is bound at runtime via <see cref="Attach" /> because <see cref="JsonRpc" /> is
///     created after the DI container is built.
/// </summary>
internal sealed class ProgressCoalescer(IJobsRepository jobsRepository, ILogger<ProgressCoalescer> logger)
{
    private static readonly TimeSpan LogFlushInterval = TimeSpan.FromSeconds(1);

    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly ConcurrentDictionary<string, RequestAggregateState> _requestStates = new();
    private GeneratingEventBus? _bus;
    private CancellationTokenSource? _cts;

    private JsonRpc? _jsonRpc;
    private LogNotifier? _logNotifier;

    /// <summary>
    ///     Attaches this coalescer to <paramref name="bus" />/<paramref name="logNotifier" />/
    ///     <see cref="IJobsRepository.Flushed" />, binds it to the active JSON-RPC connection, then starts
    ///     the ~1s log-flush loop. Must be called before the first job is started so no events are missed.
    /// </summary>
    public void Attach(GeneratingEventBus bus, LogNotifier logNotifier, JsonRpc jsonRpc)
    {
        _jsonRpc = jsonRpc;
        _bus = bus;
        _logNotifier = logNotifier;

        bus.OnRequestProgress += HandleRequest;
        bus.OnJobProgress += TrackRequestAggregate;
        bus.OnRowProgress += HandleRow;
        bus.OnExpectedJobCount += HandleExpectedJobCount;
        logNotifier.OnLogEntry += HandleLog;
        jobsRepository.Flushed += HandleJobsFlushed;

        _cts = new CancellationTokenSource();
        _ = RunLogFlushLoopAsync(_cts.Token);
    }

    /// <summary>
    ///     Detaches this coalescer, flushes the log queue one last time, then stops the flush loop. Call
    ///     during graceful shutdown before the host is stopped. <see cref="IJobsRepository" /> flushes
    ///     itself independently (see <c>JobRunner.ShutdownAsync</c>) — nothing to do for Jobs here.
    /// </summary>
    public async Task DetachAsync()
    {
        if (_bus != null)
        {
            _bus.OnRequestProgress -= HandleRequest;
            _bus.OnJobProgress -= TrackRequestAggregate;
            _bus.OnRowProgress -= HandleRow;
            _bus.OnExpectedJobCount -= HandleExpectedJobCount;
        }

        if (_logNotifier != null) _logNotifier.OnLogEntry -= HandleLog;
        jobsRepository.Flushed -= HandleJobsFlushed;

        await _cts?.CancelAsync();

        try
        {
            await FlushLogsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Final log flush on detach failed.");
        }
    }

    #region Event handlers

    private void HandleRequest(RequestProgress progress)
    {
        _ = NotifyAsync("progress/request", [progress]);
    }

    private void HandleRow(RowProgress progress)
    {
        _ = NotifyAsync("progress/rows", [progress]);
    }

    private void HandleLog(LogEntry entry)
    {
        _logQueue.Enqueue(entry);
    }

    private void HandleJobsFlushed(IReadOnlyList<JobSnapshot> batch)
    {
        _ = NotifyAsync("progress/jobs", batch);
    }

    private void HandleExpectedJobCount(string requestId, int count)
    {
        _requestStates.GetOrAdd(requestId, _ => new RequestAggregateState()).ExpectedJobCount = count;
    }

    #endregion

    #region RequestPhase aggregation

    /// <summary>
    ///     Tracks per-request job-set state and infers <see cref="RequestPhase.ProcessingStarted" />/
    ///     <see cref="RequestPhase.Completed" /> transitions, in addition to enqueueing the job itself for
    ///     the buffered <c>Jobs</c> persistence path. <see cref="RequestPhase.PreparationStarted" /> is
    ///     instead published directly by <c>Service.CreateAsync</c> and simply passes through
    ///     <see cref="HandleRequest" />. The denominator for both transitions is
    ///     <see cref="RequestAggregateState.ExpectedJobCount" /> (announced by <c>Service.CreateAsync</c>
    ///     before its spawn loop) rather than however many jobs have been observed so far.
    /// </summary>
    private void TrackRequestAggregate(JobSnapshot job)
    {
        jobsRepository.Enqueue(job);

        var state = _requestStates.GetOrAdd(job.RequestId, _ => new RequestAggregateState());
        state.KnownJobs.TryAdd(job.JobId, 0);
        if (job.JobStatus != JobStatus.Pending) state.StartedJobs.TryAdd(job.JobId, 0);
        if (job.JobStatus is JobStatus.Complete or JobStatus.Cancelled or JobStatus.Error)
            state.TerminalJobs.TryAdd(job.JobId, 0);

        var expected = state.ExpectedJobCount ?? state.KnownJobs.Count;

        if (!state.ProcessingStartedSent && expected > 0 && state.StartedJobs.Count == expected)
        {
            state.ProcessingStartedSent = true;
            HandleRequest(new RequestProgress
            {
                RequestId = job.RequestId,
                Phase = RequestPhase.ProcessingStarted,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        if (!state.CompletedSent && expected > 0 && state.TerminalJobs.Count == expected)
        {
            state.CompletedSent = true;
            HandleRequest(new RequestProgress
            {
                RequestId = job.RequestId,
                Phase = RequestPhase.Completed,
                Timestamp = DateTimeOffset.UtcNow
            });
            _requestStates.TryRemove(job.RequestId, out _);
        }
    }

    private sealed class RequestAggregateState
    {
        public readonly ConcurrentDictionary<int, byte> KnownJobs = new();
        public readonly ConcurrentDictionary<int, byte> StartedJobs = new();
        public readonly ConcurrentDictionary<int, byte> TerminalJobs = new();
        public bool CompletedSent;
        public int? ExpectedJobCount;
        public bool ProcessingStartedSent;
    }

    #endregion

    #region Log flush loop

    private async Task RunLogFlushLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(LogFlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                try
                {
                    await FlushLogsAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Log flush failed; will retry on the next tick.");
                }
        }
        catch (OperationCanceledException)
        {
            // Expected on Detach.
        }
    }

    private async Task FlushLogsAsync()
    {
        var logs = new List<LogEntry>();
        while (_logQueue.TryDequeue(out var entry)) logs.Add(entry);
        if (logs.Count > 0) await NotifyAsync("log/entries", logs).ConfigureAwait(false);
    }

    /// <summary>
    ///     Sends <paramref name="payload" /> as the single positional argument of a <paramref name="method" />
    ///     notification — i.e. JSON-RPC <c>"params": [[...]]</c>. Deliberately not
    ///     <c>NotifyWithParameterObjectAsync</c>, which would marshal a <see cref="List{T}" /> as an empty
    ///     object instead of the array a batch payload needs.
    /// </summary>
    private async Task NotifyAsync<T>(string method, IReadOnlyList<T> payload)
    {
        if (_jsonRpc is null) return;
        try
        {
            await _jsonRpc.NotifyAsync(method, payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {Method} notification.", method);
        }
    }

    #endregion
}