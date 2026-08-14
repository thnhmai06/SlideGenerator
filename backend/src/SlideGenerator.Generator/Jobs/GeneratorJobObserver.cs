/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratorJobObserver.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Jobs.Engine;

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     Maps <see cref="IJobEngine{TKey,TState}" />'s generic lifecycle callbacks onto
///     <see cref="IJobsRepository" />/<see cref="IEventBus" /> — the only place <see cref="JobStatus" />
///     values get assigned. The enqueue (persist) and publish (event) side effects below are not atomic
///     with each other; that is an accepted application-level integration detail, not a guarantee
///     <c>SlideGenerator.Jobs</c> makes.
/// </summary>
internal sealed class GeneratorJobObserver(
    IJobsRepository jobsRepository,
    IEventBus eventBus,
    ILogger<GeneratorJobObserver> logger) : IJobObserver<JobKey, JobSnapshot>
{
    /// <inheritdoc />
    public Task OnProgressAsync(JobKey key, JobSnapshot state, bool durable, CancellationToken ct) =>
        PersistAndPublishAsync(state with { JobStatus = JobStatus.Running }, durable, ct);

    /// <inheritdoc />
    public Task OnPausedAsync(JobKey key, JobSnapshot state, CancellationToken ct) =>
        PersistAndPublishAsync(state with { JobStatus = JobStatus.Paused, Timestamp = DateTimeOffset.UtcNow },
            durable: false, ct);

    /// <inheritdoc />
    public Task OnResumedAsync(JobKey key, JobSnapshot state, CancellationToken ct) =>
        PersistAndPublishAsync(state with { JobStatus = JobStatus.Running, Timestamp = DateTimeOffset.UtcNow },
            durable: false, ct);

    /// <inheritdoc />
    public async Task OnTerminalAsync(JobKey key, JobTerminalResult<JobSnapshot> result, CancellationToken ct)
    {
        var status = result.Outcome switch
        {
            JobOutcome.Completed => JobStatus.Complete,
            JobOutcome.Cancelled => JobStatus.Cancelled,
            JobOutcome.Faulted => JobStatus.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
        };
        if (result.Outcome == JobOutcome.Faulted)
            logger.LogError(result.Exception, "Job {RequestId}/{JobId} failed.", key.RequestId, key.JobId);

        await PersistAndPublishAsync(result.State with { JobStatus = status, Timestamp = DateTimeOffset.UtcNow },
            durable: true, ct).ConfigureAwait(false);
        CleanupJobTempFolder(key.RequestId, key.JobId);
    }

    private async Task PersistAndPublishAsync(JobSnapshot state, bool durable, CancellationToken ct)
    {
        jobsRepository.Enqueue(state);
        if (durable) await jobsRepository.FlushAsync(ct).ConfigureAwait(false);
        eventBus.Publish(state);
    }

    /// <summary>Deletes the per-job download folder after a terminal (non-Paused) outcome.</summary>
    private void CleanupJobTempFolder(string requestId, int jobId)
    {
        var dir = JobTempFolder.GetPath(requestId, jobId);
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up job temp folder '{Dir}'.", dir);
        }
    }
}
