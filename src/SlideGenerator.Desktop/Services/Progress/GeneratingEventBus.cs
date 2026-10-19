/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: GeneratingEventBus.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop.Services.Progress;

/// <summary>
///     A lightweight, in-process event bus for Request/Workflow/Row progress notifications.
///     Decouples publishers in <c>SlideGenerator.Generator</c> from whichever ViewModel subscribes.
/// </summary>
internal sealed class GeneratingEventBus : IEventBus
{
    /// <summary>Publishes a <see cref="RequestProgress" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(RequestProgress progress)
    {
        OnRequestProgress?.Invoke(progress);
    }

    /// <summary>Publishes a <see cref="WorkflowSnapshot" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(WorkflowSnapshot workflow)
    {
        OnWorkflowProgress?.Invoke(workflow);
    }

    /// <summary>Publishes a <see cref="RowProgress" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(RowProgress progress)
    {
        OnRowProgress?.Invoke(progress);
    }

    /// <inheritdoc />
    public void AnnounceExpectedWorkflowCount(string requestId, int count)
    {
        OnExpectedWorkflowCount?.Invoke(requestId, count);
    }

    /// <summary>Raised whenever a request-scoped progress event occurs. Subscribe before the first workflow starts.</summary>
    public event Action<RequestProgress>? OnRequestProgress;

    /// <summary>Raised whenever a workflow-scoped progress event occurs. Subscribe before the first workflow starts.</summary>
    public event Action<WorkflowSnapshot>? OnWorkflowProgress;

    /// <summary>Raised whenever a row-scoped progress event occurs. Subscribe before the first workflow starts.</summary>
    public event Action<RowProgress>? OnRowProgress;

    /// <summary>Raised whenever <see cref="AnnounceExpectedWorkflowCount" /> is called. Subscribe before the first workflow starts.</summary>
    public event Action<string, int>? OnExpectedWorkflowCount;
}