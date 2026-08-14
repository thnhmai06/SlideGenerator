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

using SlideGenerator.Generator.Job.Models;
using SlideGenerator.Generator.Job.Workload;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Jobs.Engine;
using SlideGenerator.Logging.FileLogging;

namespace SlideGenerator.Generator.Job;

/// <summary>
///     Runs jobs by delegating to <see cref="IJobEngine{TKey,TState}" /> (the generic scheduler/lifecycle
///     engine, in <c>SlideGenerator.Jobs</c>) with a <see cref="SlideGenerationWorkload" /> (the 4-phase
///     slide-generation logic) wrapped in a <see cref="LoggingWorkload" />. This type itself is a thin
///     adapter — its public contract is unchanged from before the Engine/Workload split, so
///     <c>Service</c>/<c>Program.Startup.cs</c>/existing tests mocking <see cref="IJobRunner" /> don't need
///     to change.
/// </summary>
public interface IJobRunner
{
    /// <summary>Resumes any non-terminal job found in storage (crash recovery). Call once at startup.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Requests cancellation of every running job and waits for them to unwind. Call at shutdown.</summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers and starts a fresh job. Persists its initial <see cref="JobSnapshot" /> (flushed
    ///     immediately, not on the next tick — so it is visible to conflict checks on newly-created
    ///     requests right away) before starting execution, then returns without waiting for it to finish.
    /// </summary>
    Task StartJobAsync(string requestId, int jobId, JobSpecification spec, string logPath, CancellationToken ct = default);

    /// <summary>Pauses a running job at its next row/phase checkpoint. Returns <see langword="false" /> if not currently running.</summary>
    Task<bool> PauseJobAsync(string requestId, int jobId);

    /// <summary>Resumes a paused job. Returns <see langword="false" /> if not currently paused.</summary>
    Task<bool> ResumeJobAsync(string requestId, int jobId);

    /// <summary>Cancels a running or paused job. Returns <see langword="false" /> if already terminal.</summary>
    Task<bool> StopJobAsync(string requestId, int jobId);
}

/// <inheritdoc cref="IJobRunner" />
internal sealed class JobRunner(
    IJobEngine<JobKey, JobSnapshot> engine,
    IJobResumeSource<JobKey, JobSnapshot> resumeSource,
    SlideGenerationWorkload slideGenerationWorkload,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier,
    IJobsRepository jobsRepository) : IJobRunner
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken ct = default) => engine.InitializeAsync(resumeSource, ct);

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        // Ordering matters: the engine guarantees no workload is still calling ReportAsync once
        // ShutdownAsync returns, so it is safe to flush right after.
        await engine.ShutdownAsync(ct).ConfigureAwait(false);
        await jobsRepository.FlushAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StartJobAsync(
        string requestId, int jobId, JobSpecification spec, string logPath, CancellationToken ct = default)
    {
        var initial = new JobSnapshot(requestId, jobId, JobStatus.Pending, JobPhase.CreatingOutput, 0, spec,
            DateTimeOffset.UtcNow);
        jobsRepository.Enqueue(initial);
        await jobsRepository.FlushAsync(ct).ConfigureAwait(false);

        var workload = new LoggingWorkload(logPath, slideGenerationWorkload, fileLoggerFactory, logNotifier);
        await engine.StartJobAsync((requestId, jobId), initial, workload, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> PauseJobAsync(string requestId, int jobId) => engine.PauseAsync((requestId, jobId));

    /// <inheritdoc />
    public Task<bool> ResumeJobAsync(string requestId, int jobId) => engine.ResumeAsync((requestId, jobId));

    /// <inheritdoc />
    public Task<bool> StopJobAsync(string requestId, int jobId) => engine.StopAsync((requestId, jobId));
}
