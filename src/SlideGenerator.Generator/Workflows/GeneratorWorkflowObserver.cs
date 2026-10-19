/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratorWorkflowObserver.cs
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
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Maps <see cref="IWorkflowEngine{TKey,TState}" />'s generic lifecycle callbacks onto
///     <see cref="IWorkflowsRepository" />/<see cref="IEventBus" /> — the only place a workflow's own
///     <see cref="JobStatus" /> is assigned. (Job statuses are assigned by <see cref="SlideGenerationWorkflow" />.)
///     The persist and publish side effects below are not atomic with each other; that is an accepted
///     application-level integration detail, not a guarantee <c>SlideGenerator.Workflows</c> makes.
/// </summary>
internal sealed class GeneratorWorkflowObserver(
    IWorkflowsRepository workflowsRepository,
    IEventBus eventBus,
    ILogger<GeneratorWorkflowObserver> logger) : IWorkflowObserver<string, WorkflowSnapshot>
{
    /// <inheritdoc />
    public Task OnProgressAsync(string key, WorkflowSnapshot state, bool durable, CancellationToken ct)
    {
        return PersistAndPublishAsync(state with { Status = JobStatus.Running, Timestamp = DateTimeOffset.UtcNow }, ct);
    }

    /// <inheritdoc />
    public Task OnPausedAsync(string key, WorkflowSnapshot state, CancellationToken ct)
    {
        return PersistAndPublishAsync(state with { Status = JobStatus.Paused, Timestamp = DateTimeOffset.UtcNow }, ct);
    }

    /// <inheritdoc />
    public Task OnResumedAsync(string key, WorkflowSnapshot state, CancellationToken ct)
    {
        return PersistAndPublishAsync(state with { Status = JobStatus.Running, Timestamp = DateTimeOffset.UtcNow }, ct);
    }

    /// <inheritdoc />
    public Task OnTerminalAsync(string key, WorkflowTerminalResult<WorkflowSnapshot> result, CancellationToken ct)
    {
        var status = result.Outcome switch
        {
            WorkflowOutcome.Completed => JobStatus.Complete,
            WorkflowOutcome.Cancelled => JobStatus.Cancelled,
            WorkflowOutcome.Faulted => JobStatus.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
        };
        if (result.Outcome == WorkflowOutcome.Faulted)
            logger.LogError(result.Exception, "Workflow {WorkflowId} failed.", key);

        return PersistAndPublishAsync(
            result.State with { Status = status, Timestamp = DateTimeOffset.UtcNow }, ct);
    }

    private async Task PersistAndPublishAsync(WorkflowSnapshot state, CancellationToken ct)
    {
        await workflowsRepository.UpdateStatusAsync(state.WorkflowId, state.Status, ct).ConfigureAwait(false);
        eventBus.Publish(state);
    }
}
