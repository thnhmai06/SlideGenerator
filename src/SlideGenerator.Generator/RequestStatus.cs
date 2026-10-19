/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: RequestStatus.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;

namespace SlideGenerator.Generator;

/// <summary>Aggregate execution status of one request (a workflow and its jobs), derived from the job statuses and never persisted.</summary>
public enum RequestStatus : byte
{
    /// <summary>At least one job is running or still waiting to start.</summary>
    Running,
    /// <summary>The workflow was paused, or no job is running and at least one is paused.</summary>
    Paused,
    /// <summary>Every job finished without error (some may have been cancelled).</summary>
    Complete,
    /// <summary>Every job finished, at least one ended in error and at least one did not.</summary>
    CompletedWithError,
    /// <summary>Every job ended in error.</summary>
    Error,
    /// <summary>Every job was cancelled.</summary>
    Cancelled
}

/// <summary>Derives a request's <see cref="RequestStatus" /> from its jobs — the single definition shared by the service and the UI.</summary>
public static class RequestStatusRules
{
    /// <summary>
    ///     Derives the status. <paramref name="workflowPaused" /> reflects a pause applied to the whole workflow, which does not rewrite job rows, so
    ///     it turns an otherwise running request into <see cref="RequestStatus.Paused" />. An empty job list is <see cref="RequestStatus.Complete" />.
    /// </summary>
    public static RequestStatus Derive(IReadOnlyCollection<JobStatus> jobs, bool workflowPaused = false)
    {
        if (jobs.Count == 0) return RequestStatus.Complete;
        if (jobs.Any(s => s is JobStatus.Running or JobStatus.Pending))
            return workflowPaused ? RequestStatus.Paused : RequestStatus.Running;
        if (jobs.Any(s => s == JobStatus.Paused)) return RequestStatus.Paused;
        if (jobs.All(s => s == JobStatus.Cancelled)) return RequestStatus.Cancelled;
        if (jobs.All(s => s == JobStatus.Error)) return RequestStatus.Error;
        return jobs.Any(s => s == JobStatus.Error) ? RequestStatus.CompletedWithError : RequestStatus.Complete;
    }
}
