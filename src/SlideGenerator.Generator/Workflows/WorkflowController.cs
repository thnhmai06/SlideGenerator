/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowController.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Logging.FileLogging;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Starts and controls workflows by delegating to <see cref="IWorkflowEngine{TKey,TState}" /> (the generic
///     lifecycle engine, in <c>SlideGenerator.Workflows</c>) with a <see cref="SlideGenerationWorkflow" /> wrapped in a
///     <see cref="LoggingWorkflowRunner" />. This type itself is a thin adapter. Pause, resume and stop act on a
///     whole workflow (all of its jobs at once) — there is no per-job control.
/// </summary>
public interface IWorkflowController
{
    /// <summary>Resumes any non-terminal workflow found in storage (crash recovery). Call once at startup.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Requests cancellation of every running workflow and waits for them to unwind. Call at shutdown.</summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers a fresh workflow: persists one <see cref="JobStatus.Pending" /> job row per entry of
    ///     <paramref name="jobs" /> (flushed immediately, so they are visible to conflict checks right away), then
    ///     starts execution and returns without waiting for it to finish. The workflow's own row must already exist.
    /// </summary>
    Task StartWorkflowAsync(string workflowId, IReadOnlyList<JobSpecification> jobs, string logPath,
        CancellationToken ct = default);

    /// <summary>Pauses a running workflow at its next checkpoint. Returns <see langword="false" /> if not currently running.</summary>
    Task<bool> PauseWorkflowAsync(string workflowId);

    /// <summary>Resumes a paused workflow. Returns <see langword="false" /> if not currently running.</summary>
    Task<bool> ResumeWorkflowAsync(string workflowId);

    /// <summary>Cancels a running or paused workflow. Returns <see langword="false" /> if not currently running.</summary>
    Task<bool> StopWorkflowAsync(string workflowId);
}

/// <inheritdoc cref="IWorkflowController" />
internal sealed class WorkflowController(
    IWorkflowEngine<string, WorkflowSnapshot> engine,
    IWorkflowResumeSource<string, WorkflowSnapshot> resumeSource,
    SlideGenerationWorkflow slideGenerationWorkflow,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier,
    IJobsRepository jobsRepository) : IWorkflowController
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken ct = default)
    {
        return engine.InitializeAsync(resumeSource, ct);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        // Ordering matters: the engine guarantees no runner is still reporting once ShutdownAsync returns, so it
        // is safe to flush right after.
        await engine.ShutdownAsync(ct).ConfigureAwait(false);
        await jobsRepository.FlushAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StartWorkflowAsync(
        string workflowId, IReadOnlyList<JobSpecification> jobs, string logPath, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        for (var jobId = 0; jobId < jobs.Count; jobId++)
            jobsRepository.Enqueue(new JobSnapshot(workflowId, jobId, JobStatus.Pending, JobPhase.CreatingOutput, 0,
                jobs[jobId], now));
        await jobsRepository.FlushAsync(ct).ConfigureAwait(false);

        var runner = new LoggingWorkflowRunner(logPath, slideGenerationWorkflow, fileLoggerFactory, logNotifier);
        await engine.StartWorkflowAsync(workflowId, new WorkflowSnapshot(workflowId, JobStatus.Pending, now), runner, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> PauseWorkflowAsync(string workflowId)
    {
        return engine.PauseAsync(workflowId);
    }

    /// <inheritdoc />
    public Task<bool> ResumeWorkflowAsync(string workflowId)
    {
        return engine.ResumeAsync(workflowId);
    }

    /// <inheritdoc />
    public Task<bool> StopWorkflowAsync(string workflowId)
    {
        return engine.StopAsync(workflowId);
    }
}
