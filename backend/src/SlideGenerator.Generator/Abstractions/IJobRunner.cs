/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IJobRunner.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Models.Data;

namespace SlideGenerator.Generator.Abstractions;

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
