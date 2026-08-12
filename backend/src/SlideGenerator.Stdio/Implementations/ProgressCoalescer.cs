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
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Generator.Models.Enum;
using StreamJsonRpc;

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     Subscribes to <see cref="GeneratingEventBus" />/<see cref="LogNotifier" />, coalesces Progress
///     (current-state, last-write-wins per key) and buffers Logs (append-only, never dropped), then every
///     ~1s: upserts dirty Progress into <see cref="IStudioRepository" /> and forwards both channels as
///     JSON-RPC notifications (<c>progress/request</c>, <c>progress/jobs</c>, <c>progress/rows</c>,
///     <c>log/entries</c>) via the active <see cref="JsonRpc" /> connection. The connection is bound at
///     runtime via <see cref="Attach" /> rather than at construction time because <see cref="JsonRpc" /> is
///     created after the DI container is built.
/// </summary>
internal sealed class ProgressCoalescer(IStudioRepository studioRepository, ILogger<ProgressCoalescer> logger)
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

    private ConcurrentDictionary<string, RequestProgress> _dirtyRequests = new();
    private ConcurrentDictionary<string, JobProgress> _dirtyJobs = new();
    private ConcurrentDictionary<string, RowProgress> _dirtyRows = new();
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly ConcurrentDictionary<string, RequestAggregateState> _requestStates = new();

    private JsonRpc? _jsonRpc;
    private CancellationTokenSource? _cts;
    private GeneratingEventBus? _bus;
    private LogNotifier? _logNotifier;

    /// <summary>
    ///     Attaches this coalescer to <paramref name="bus" />/<paramref name="logNotifier" /> and binds it
    ///     to the active JSON-RPC connection, then starts the ~1s flush loop. Must be called before the
    ///     first workflow is started so no events are missed.
    /// </summary>
    public void Attach(GeneratingEventBus bus, LogNotifier logNotifier, JsonRpc jsonRpc)
    {
        _jsonRpc = jsonRpc;
        _bus = bus;
        _logNotifier = logNotifier;

        bus.OnRequestProgress += HandleRequest;
        bus.OnJobProgress += HandleJob;
        bus.OnRowProgress += HandleRow;
        bus.OnExpectedJobCount += HandleExpectedJobCount;
        logNotifier.OnLogEntry += HandleLog;

        _cts = new CancellationTokenSource();
        _ = RunFlushLoopAsync(_cts.Token);
    }

    /// <summary>
    ///     Detaches this coalescer, flushes one last time so nothing accumulated since the previous tick
    ///     is dropped, then stops the flush loop. Call during graceful shutdown before the host is stopped.
    /// </summary>
    public async Task DetachAsync()
    {
        if (_bus != null)
        {
            _bus.OnRequestProgress -= HandleRequest;
            _bus.OnJobProgress -= HandleJob;
            _bus.OnRowProgress -= HandleRow;
            _bus.OnExpectedJobCount -= HandleExpectedJobCount;
        }

        if (_logNotifier != null)
            _logNotifier.OnLogEntry -= HandleLog;

        _cts?.Cancel();

        try
        {
            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Final flush on detach failed.");
        }
    }

    #region Event handlers

    private void HandleRequest(RequestProgress progress) => _dirtyRequests[progress.RequestId] = progress;

    private void HandleJob(JobProgress progress)
    {
        _dirtyJobs[$"{progress.RequestId}/{progress.JobId}"] = progress;
        TrackRequestAggregate(progress);
    }

    private void HandleRow(RowProgress progress) =>
        _dirtyRows[$"{progress.RequestId}/{progress.JobId}/{progress.RowIndex}"] = progress;

    private void HandleLog(LogEntry entry) => _logQueue.Enqueue(entry);

    private void HandleExpectedJobCount(string requestId, int count) =>
        _requestStates.GetOrAdd(requestId, _ => new RequestAggregateState()).ExpectedJobCount = count;

    #endregion

    #region RequestPhase aggregation

    /// <summary>
    ///     Tracks per-request job-set state (this coalescer, not <see cref="Service" />, is the sole owner
    ///     of it) and infers <see cref="RequestPhase.ProcessingStarted" />/<see cref="RequestPhase.Completed" />
    ///     transitions. <see cref="RequestPhase.PreparationStarted" /> is instead published directly by
    ///     <c>Service.CreateAsync</c> and simply passes through <see cref="HandleRequest" /> above.
    ///     "Past preparation" is approximated as "left <see cref="Status.Pending" />" — the model no longer
    ///     carries a per-job <c>Phase</c> signal (removed along with <c>StepCompleted</c> events), and a
    ///     job's <see cref="Status.Running" /> transition (via <c>WorkflowStarted</c>) is the closest
    ///     available proxy to "picked up by WorkflowCore and past its preparation step".
    ///     The denominator for both transitions is <see cref="RequestAggregateState.ExpectedJobCount" />
    ///     (set via <see cref="HandleExpectedJobCount" />, announced by <c>Service.CreateAsync</c> before
    ///     its spawn loop starts) rather than however many jobs have been observed so far — the spawn loop
    ///     awaits each <c>StartWorkflow</c> call, and WorkflowCore fires <c>WorkflowStarted</c> from its own
    ///     background poller, so job 1 can already be <see cref="Status.Running" /> while job 2 hasn't even
    ///     been spawned yet; without the announced total, that would trip <see cref="RequestPhase.ProcessingStarted" />
    ///     early for any multi-job request.
    /// </summary>
    private void TrackRequestAggregate(JobProgress progress)
    {
        var state = _requestStates.GetOrAdd(progress.RequestId, _ => new RequestAggregateState());
        state.KnownJobs.TryAdd(progress.JobId, 0);
        if (progress.Status != Status.Pending) state.StartedJobs.TryAdd(progress.JobId, 0);
        if (progress.Status is Status.Complete or Status.Cancelled or Status.Error)
            state.TerminalJobs.TryAdd(progress.JobId, 0);

        // Falls back to however many jobs have been observed so far only if the expected-count
        // announcement was somehow missed — keeps the aggregation from getting stuck forever.
        var expected = state.ExpectedJobCount ?? state.KnownJobs.Count;

        if (!state.ProcessingStartedSent && expected > 0 && state.StartedJobs.Count == expected)
        {
            state.ProcessingStartedSent = true;
            HandleRequest(new RequestProgress
            {
                RequestId = progress.RequestId,
                Phase = RequestPhase.ProcessingStarted,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        if (!state.CompletedSent && expected > 0 && state.TerminalJobs.Count == expected)
        {
            state.CompletedSent = true;
            HandleRequest(new RequestProgress
            {
                RequestId = progress.RequestId,
                Phase = RequestPhase.Completed,
                Timestamp = DateTimeOffset.UtcNow
            });
            _requestStates.TryRemove(progress.RequestId, out _);
        }
    }

    private sealed class RequestAggregateState
    {
        public int? ExpectedJobCount;
        public readonly ConcurrentDictionary<string, byte> KnownJobs = new();
        public readonly ConcurrentDictionary<string, byte> StartedJobs = new();
        public readonly ConcurrentDictionary<string, byte> TerminalJobs = new();
        public bool ProcessingStartedSent;
        public bool CompletedSent;
    }

    #endregion

    #region Flush loop

    private async Task RunFlushLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await FlushAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A single failed flush (DB I/O, etc.) must not permanently stop delivery of every
                    // subsequent progress/log event for the rest of the process lifetime.
                    logger.LogWarning(ex, "Progress flush failed; will retry on the next tick.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Detach.
        }
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var requests = DrainDirty(ref _dirtyRequests);
        var jobs = DrainDirty(ref _dirtyJobs);
        var rows = DrainDirty(ref _dirtyRows);
        var logs = DrainLogs();

        if (requests.Count > 0)
        {
            foreach (var r in requests)
                await studioRepository.UpsertRequestAsync(r, ct).ConfigureAwait(false);
            await NotifyAsync("progress/request", requests).ConfigureAwait(false);
        }

        if (jobs.Count > 0)
        {
            await studioRepository.UpsertJobsAsync(jobs, ct).ConfigureAwait(false);
            await NotifyAsync("progress/jobs", jobs).ConfigureAwait(false);
        }

        if (rows.Count > 0)
        {
            await studioRepository.UpsertRowsAsync(rows, ct).ConfigureAwait(false);
            await NotifyAsync("progress/rows", rows).ConfigureAwait(false);
        }

        if (logs.Count > 0)
            await NotifyAsync("log/entries", logs).ConfigureAwait(false);
    }

    private static List<TValue> DrainDirty<TValue>(ref ConcurrentDictionary<string, TValue> dict)
    {
        var old = Interlocked.Exchange(ref dict, new ConcurrentDictionary<string, TValue>());
        return old.Values.ToList();
    }

    private List<LogEntry> DrainLogs()
    {
        var list = new List<LogEntry>();
        while (_logQueue.TryDequeue(out var entry)) list.Add(entry);
        return list;
    }

    /// <summary>
    ///     Sends <paramref name="payload" /> as the single positional argument of a <paramref name="method" />
    ///     notification — i.e. JSON-RPC <c>"params": [[...]]</c>, one positional slot whose value is the
    ///     whole batch array. Deliberately <em>not</em> <c>NotifyWithParameterObjectAsync</c>: that method
    ///     marshals its argument using reflection over public properties (named-parameter convention) —
    ///     for a <see cref="List{T}" />/<see cref="IReadOnlyList{T}" /> argument (no serializable properties
    ///     of its own) it produces an empty JSON object rather than the array a batch payload needs.
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
