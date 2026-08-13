/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobRunner.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Events;
using SlideGenerator.Cloud;
using SlideGenerator.Document.Slides;
using SlideGenerator.Document.Template;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Image.Loading;
using SlideGenerator.Logging.FileLogging;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Generator.Job;

/// <summary>
///     Runs jobs directly on <see cref="Task.Run(Action)" />, replacing WorkflowCore. Each job runs its
///     4 phases (create/open output → create slides → fill text → fill images) sequentially in-process,
///     bounded by a concurrency semaphore sized from <c>Performance.MaxConcurrentJobs</c>, with pause/stop
///     checked between rows and phases.
/// </summary>
public interface IJobRunner
{
    /// <summary>Resumes any non-terminal job found in storage (crash recovery). Call once at startup.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Requests cancellation of every running job and waits for them to unwind. Call at shutdown.</summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers and starts a fresh job. Runs <c>PreflightCleanup</c>, persists its initial
    ///     <see cref="JobRecord" /> (flushed immediately, not on the next tick), then starts execution and
    ///     returns without waiting for it to finish.
    /// </summary>
    Task StartJobAsync(string requestId, int jobId, JobSpecification spec, string logPath, CancellationToken ct = default);

    /// <summary>Pauses a running job at its next row/phase checkpoint. Returns <see langword="false" /> if not currently running.</summary>
    Task<bool> PauseJobAsync(string requestId, int jobId);

    /// <summary>Resumes a paused job. Returns <see langword="false" /> if not currently paused.</summary>
    Task<bool> ResumeJobAsync(string requestId, int jobId);

    /// <summary>Cancels a running or paused job. Returns <see langword="false" /> if already terminal.</summary>
    Task<bool> StopJobAsync(string requestId, int jobId);
}

/// <summary>
///     Runs jobs directly on <see cref="Task.Run(Action)" />, replacing WorkflowCore entirely. See
///     <see cref="RunJobAsync" /> for the 4-phase pipeline and <c>JobRunner.Phases.cs</c> for each phase's
///     implementation (ported from the removed <c>GenerateJobStep</c>/<c>InspectUrlsStep</c>).
/// </summary>
internal sealed partial class JobRunner(
    IWorkbookOpener workbookOpener,
    IPresentationOpener presentationOpener,
    TextComposer textComposer,
    ISmartCropper smartCropper,
    IImageLoader imageLoader,
    ICloudClient cloudClient,
    IHttpClientFactory httpClientFactory,
    ISettingProvider settingProvider,
    IEventBus eventBus,
    IJobsRepository jobsRepository,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier,
    ILogger<JobRunner> logger) : IJobRunner
{
    private static readonly string[] ScopePropertyNames = ["RequestId", "JobId", "RowIndex"];

    private readonly ConcurrentDictionary<(string RequestId, int JobId), RunningJob> _running = new();
    private SemaphoreSlim _semaphore = new(1);

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        ApplyMaxConcurrentJobs();

        var crashed = await jobsRepository.GetNonTerminalAsync(ct).ConfigureAwait(false);
        foreach (var record in crashed)
        {
            // A job that was Paused before the crash resumes as Running — closing/reopening handles on
            // pause is a known limitation (see IJobRunner remarks), so there's no "paused" state to honor.
            var running = Register(record.RequestId, record.JobId);
            eventBus.Publish(record with { Status = Status.Running, Timestamp = DateTimeOffset.UtcNow });
            running.RunTask = Task.Run(() => RunJobAsync(record, isResume: true, running), CancellationToken.None);
        }

        logger.LogInformation("JobRunner initialized. Resumed {Count} non-terminal job(s).", crashed.Count);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        foreach (var job in _running.Values) job.Cts.Cancel();
        await Task.WhenAll(_running.Values.Select(j => j.RunTask ?? Task.CompletedTask))
            .ContinueWith(_ => { }, TaskScheduler.Default).ConfigureAwait(false);
        await jobsRepository.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        logger.LogInformation("JobRunner shut down.");
    }

    /// <inheritdoc />
    public async Task StartJobAsync(
        string requestId, int jobId, JobSpecification spec, string logPath, CancellationToken ct = default)
    {
        ApplyMaxConcurrentJobs();

        var record = new JobRecord(requestId, jobId, Status.Pending, JobPhase.CreatingOutput, 0, spec,
            DateTimeOffset.UtcNow);
        jobsRepository.Enqueue(record);
        await jobsRepository.FlushAsync(ct).ConfigureAwait(false);

        var running = Register(requestId, jobId);
        running.LogPath = logPath;
        running.RunTask = Task.Run(() => RunJobAsync(record, isResume: false, running), CancellationToken.None);
    }

    /// <inheritdoc />
    public Task<bool> PauseJobAsync(string requestId, int jobId)
    {
        if (!_running.TryGetValue((requestId, jobId), out var job)) return Task.FromResult(false);
        job.Gate.Pause();
        var record = job.LastRecord() with { Status = Status.Paused };
        Persist(record);
        eventBus.Publish(record);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ResumeJobAsync(string requestId, int jobId)
    {
        if (!_running.TryGetValue((requestId, jobId), out var job)) return Task.FromResult(false);
        job.Gate.Resume();
        var record = job.LastRecord() with { Status = Status.Running };
        Persist(record);
        eventBus.Publish(record);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> StopJobAsync(string requestId, int jobId)
    {
        if (!_running.TryGetValue((requestId, jobId), out var job)) return Task.FromResult(false);
        job.Cts.Cancel();
        job.Gate.Resume(); // unblock a paused checkpoint so cancellation is observed promptly
        return Task.FromResult(true);
    }

    /// <summary>
    ///     Re-applies <c>Performance.MaxConcurrentJobs</c> from current settings onto <see cref="_semaphore" />.
    ///     Swaps in a new semaphore instance rather than mutating the old one, so jobs already waiting on
    ///     the previous instance are unaffected by the resize (only newly-queued waits see the new limit).
    /// </summary>
    private void ApplyMaxConcurrentJobs()
    {
        var value = (int)settingProvider.Current.Performance.MaxConcurrentJobs;
        _semaphore = new SemaphoreSlim(value, value);
        logger.LogInformation("MaxConcurrentJobs set to {Value}.", value);
    }

    private RunningJob Register(string requestId, int jobId)
    {
        var job = new RunningJob { Cts = new CancellationTokenSource(), RequestId = requestId, JobId = jobId };
        _running[(requestId, jobId)] = job;
        return job;
    }

    /// <summary>
    ///     Runs every phase of one job to completion (or until paused/cancelled/failed), publishing status
    ///     transitions and cleaning up on any terminal outcome. Semaphore-gated so at most
    ///     <c>Performance.MaxConcurrentJobs</c> jobs execute concurrently.
    /// </summary>
    private async Task RunJobAsync(JobRecord initial, bool isResume, RunningJob running)
    {
        var requestId = initial.RequestId;
        var jobId = initial.JobId;
        running.LastSpec = initial.Specification;
        running.LastPhase = initial.Phase;
        running.LastIndex = initial.CurrentIndex;

        eventBus.Publish(initial with { Status = Status.Running, Timestamp = DateTimeOffset.UtcNow });

        await _semaphore.WaitAsync(running.Cts.Token).ConfigureAwait(false);
        try
        {
            using var loggerFactory = fileLoggerFactory.CreateFile(running.LogPath ?? DefaultLogPath(requestId),
                ScopePropertyNames, notification => logNotifier.Publish(new LogEntry
                {
                    Timestamp = notification.Timestamp,
                    Path = notification.Location,
                    Level = notification.Level switch
                    {
                        LogEventLevel.Verbose => "VRB",
                        LogEventLevel.Debug => "DBG",
                        LogEventLevel.Information => "INF",
                        LogEventLevel.Warning => "WRN",
                        LogEventLevel.Error => "ERR",
                        LogEventLevel.Fatal => "FTL",
                        _ => "???"
                    },
                    Info = notification.Message
                }));
            var jobLogger = loggerFactory.CreateLogger(nameof(JobRunner));

            using var requestScope = LogContext.PushProperty("RequestId", requestId);
            using var jobScope = LogContext.PushProperty("JobId", jobId);

            if (!isResume) PreflightCleanup.Run(initial.Specification.OutputPath, jobLogger);

            var final = await RunPhasesAsync(initial, running, jobLogger).ConfigureAwait(false);
            Persist(final);
            eventBus.Publish(final);
            CleanupJobTempFolder(requestId, jobId);
        }
        catch (OperationCanceledException) when (running.Cts.IsCancellationRequested)
        {
            var cancelled = running.LastRecord() with { Status = Status.Cancelled, Timestamp = DateTimeOffset.UtcNow };
            Persist(cancelled);
            eventBus.Publish(cancelled);
            CleanupJobTempFolder(requestId, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {RequestId}/{JobId} failed.", requestId, jobId);
            var errored = running.LastRecord() with { Status = Status.Error, Timestamp = DateTimeOffset.UtcNow };
            Persist(errored);
            eventBus.Publish(errored);
            CleanupJobTempFolder(requestId, jobId);
        }
        finally
        {
            _semaphore.Release();
            _running.TryRemove((requestId, jobId), out _);
        }
    }

    private void Persist(JobRecord record)
    {
        jobsRepository.Enqueue(record);
    }

    private static string DefaultLogPath(string requestId) =>
        Path.Combine(NameAndPaths.LogsFolder.WorkflowPath, $"{requestId}.log");

    /// <summary>
    ///     Per-job download cache folder: <c>%TEMP%\SlideGenerator\{requestId}\{jobId}\</c>. Kept isolated
    ///     per job so concurrent jobs never contend on the same file (see <c>Utilities.cs</c>'s ponytail
    ///     note on dropping the old file-lock).
    /// </summary>
    internal static string JobTempFolder(string requestId, int jobId) =>
        Path.Combine(NameAndPaths.TempFolder.RootPath, requestId, jobId.ToString());

    /// <summary>Deletes the per-job download folder after a terminal (non-Paused) outcome.</summary>
    private void CleanupJobTempFolder(string requestId, int jobId)
    {
        var dir = JobTempFolder(requestId, jobId);
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up job temp folder '{Dir}'.", dir);
        }
    }

    /// <summary>In-memory registry entry for one currently-running (or resumed) job.</summary>
    private sealed class RunningJob
    {
        public required CancellationTokenSource Cts { get; init; }
        public PauseGate Gate { get; } = new();
        public Task? RunTask { get; set; }
        public string? LogPath { get; set; }
        public JobSpecification? LastSpec { get; set; }
        public JobPhase LastPhase { get; set; }
        public int LastIndex { get; set; }
        public string RequestId { get; set; } = "";
        public int JobId { get; set; }

        public JobRecord LastRecord() => new(RequestId, JobId, Status.Running, LastPhase, LastIndex, LastSpec!,
            DateTimeOffset.UtcNow);
    }
}

/// <summary>
///     Swappable pause checkpoint: <see cref="Pause" />/<see cref="Resume" /> toggle a shared signal;
///     <see cref="CheckpointAsync" /> blocks the caller while paused. Checked between rows and at phase
///     boundaries — never mid-row — for pause granularity "between each row" as specified.
/// </summary>
internal sealed class PauseGate
{
    private TaskCompletionSource<bool>? _pauseSignal;

    public void Pause() =>
        Interlocked.CompareExchange(ref _pauseSignal,
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously), null);

    public void Resume() => Interlocked.Exchange(ref _pauseSignal, null)?.TrySetResult(true);

    public async Task CheckpointAsync(CancellationToken ct)
    {
        var signal = _pauseSignal;
        if (signal is null) return;
        await using var registration = ct.Register(() => signal.TrySetCanceled(ct));
        await signal.Task.ConfigureAwait(false);
    }
}
