/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: GeneratingEventBus.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     A lightweight, in-process event bus for Request/Job/Row progress notifications.
///     Decouples publishers in <c>SlideGenerator.Generator</c> from <see cref="ProgressCoalescer" />
///     (subscriber) without depending on WorkflowCore-internal lifecycle hooks.
/// </summary>
internal sealed class GeneratingEventBus : IEventBus
{
    /// <summary>Publishes a <see cref="RequestProgress" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(RequestProgress progress)
    {
        OnRequestProgress?.Invoke(progress);
    }

    /// <summary>Publishes a <see cref="JobSnapshot" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(JobSnapshot job)
    {
        OnJobProgress?.Invoke(job);
    }

    /// <summary>Publishes a <see cref="RowProgress" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(RowProgress progress)
    {
        OnRowProgress?.Invoke(progress);
    }

    /// <inheritdoc />
    public void AnnounceExpectedJobCount(string requestId, int count)
    {
        OnExpectedJobCount?.Invoke(requestId, count);
    }

    /// <summary>Raised whenever a request-scoped progress event occurs. Subscribe before the first workflow starts.</summary>
    public event Action<RequestProgress>? OnRequestProgress;

    /// <summary>Raised whenever a job-scoped progress event occurs. Subscribe before the first workflow starts.</summary>
    public event Action<JobSnapshot>? OnJobProgress;

    /// <summary>Raised whenever a row-scoped progress event occurs. Subscribe before the first workflow starts.</summary>
    public event Action<RowProgress>? OnRowProgress;

    /// <summary>Raised whenever <see cref="AnnounceExpectedJobCount" /> is called. Subscribe before the first workflow starts.</summary>
    public event Action<string, int>? OnExpectedJobCount;
}