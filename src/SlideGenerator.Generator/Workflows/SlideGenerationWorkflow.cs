/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: SlideGenerationWorkflow.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     The workflow for one <see cref="Request" />: runs every one of its jobs (one per worksheet/presentation
///     pair) concurrently, each through a <see cref="JobExecutor" /> and each throttled by the shared
///     <see cref="JobGate" />. The only place a <see cref="JobStatus" /> is assigned to a <see cref="JobSnapshot" />.
/// </summary>
/// <remarks>
///     A job that fails is marked <see cref="JobStatus.Error" /> and does not stop its siblings; cancelling the
///     workflow marks every job that has not finished <see cref="JobStatus.Cancelled" /> and rethrows so the engine
///     reports the workflow itself as cancelled. Pause is cooperative and workflow-wide: every job checkpoints
///     through the same <see cref="IWorkflowContext{TState}" />.
/// </remarks>
internal sealed class SlideGenerationWorkflow(
    IJobsRepository jobsRepository,
    JobExecutor jobExecutor,
    JobGate jobGate,
    IEventBus eventBus,
    ILogger<SlideGenerationWorkflow> logger) : IWorkflowRunner<WorkflowSnapshot>
{
    /// <inheritdoc />
    public async Task<WorkflowSnapshot> RunAsync(
        WorkflowSnapshot state, IWorkflowContext<WorkflowSnapshot> context, CancellationToken ct)
    {
        var workflowLogger = context is IScopedLoggerContext scoped ? scoped.Logger : logger;
        var jobs = await jobsRepository.GetByWorkflowIdAsync(state.WorkflowId, ct).ConfigureAwait(false);

        // Pending/Running/Paused rows are unfinished: a fresh workflow has only Pending ones, a resumed one may
        // have jobs part-way through (Phase + CurrentIndex say where to continue).
        var unfinished = jobs.Where(j => j.JobStatus is JobStatus.Pending or JobStatus.Running or JobStatus.Paused);
        await Task.WhenAll(unfinished.Select(job =>
            Task.Run(() => RunJobAsync(job, context, workflowLogger, ct), CancellationToken.None))).ConfigureAwait(false);

        return state with { Timestamp = DateTimeOffset.UtcNow };
    }

    private async Task RunJobAsync(
        JobSnapshot job, IWorkflowContext<WorkflowSnapshot> context, ILogger workflowLogger, CancellationToken ct)
    {
        using var jobScope = LogContext.PushProperty("JobId", job.JobId);
        var jobContext = new JobContext(job, context, workflowLogger, this);
        var completed = false;
        try
        {
            using var lease = await jobGate.AcquireAsync(ct).ConfigureAwait(false);

            // A job that was queued for a slot while the workflow was paused must not start until it resumes.
            await context.CheckpointAsync(ct).ConfigureAwait(false);

            var final = await jobExecutor.RunAsync(job, jobContext, ct).ConfigureAwait(false);
            await PersistAndPublishAsync(final with { JobStatus = JobStatus.Complete, Timestamp = DateTimeOffset.UtcNow },
                true, CancellationToken.None).ConfigureAwait(false);
            completed = true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await PersistAndPublishAsync(
                jobContext.Last with { JobStatus = JobStatus.Cancelled, Timestamp = DateTimeOffset.UtcNow },
                true, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            workflowLogger.LogError(ex, "Job {JobId} failed.", job.JobId);
            await PersistAndPublishAsync(
                jobContext.Last with { JobStatus = JobStatus.Error, Timestamp = DateTimeOffset.UtcNow },
                true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            CleanupJobTempFolder(job.WorkflowId, job.JobId);

            // A job that never completed must not leave its working file behind (a crash never reaches this
            // point, so a crashed job keeps the .partial file it can resume from).
            if (!completed) JobOutput.DiscardPartial(job.Specification.OutputPath, workflowLogger);
        }
    }

    private async Task PersistAndPublishAsync(JobSnapshot snapshot, bool durable, CancellationToken ct)
    {
        jobsRepository.Enqueue(snapshot);
        if (durable) await jobsRepository.FlushAsync(ct).ConfigureAwait(false);
        eventBus.Publish(snapshot);
    }

    /// <summary>Deletes the per-job download folder once the job has finished, whatever its outcome.</summary>
    private void CleanupJobTempFolder(string workflowId, int jobId)
    {
        var dir = JobTempFolder.GetPath(workflowId, jobId);
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up job temp folder '{Dir}'.", dir);
        }
    }

    /// <summary>Bridges one job to its workflow: reports persist/publish its snapshot, checkpoints use the workflow's gate.</summary>
    private sealed class JobContext(
        JobSnapshot initial, IWorkflowContext<WorkflowSnapshot> workflow, ILogger logger,
        SlideGenerationWorkflow owner) : IJobContext
    {
        private readonly Lock _lock = new();
        private JobSnapshot _last = initial;

        /// <summary>The last state this job reported (or its initial state if it never reported).</summary>
        public JobSnapshot Last
        {
            get
            {
                lock (_lock)
                {
                    return _last;
                }
            }
        }

        public bool IsResume => workflow.IsResume;

        public ILogger Logger => logger;

        public Task ReportAsync(JobSnapshot state, bool durable = false, CancellationToken ct = default)
        {
            var running = state with { JobStatus = JobStatus.Running };
            lock (_lock)
            {
                _last = running;
            }

            return owner.PersistAndPublishAsync(running, durable, ct);
        }

        public Task CheckpointAsync(CancellationToken ct)
        {
            return workflow.CheckpointAsync(ct);
        }
    }
}
