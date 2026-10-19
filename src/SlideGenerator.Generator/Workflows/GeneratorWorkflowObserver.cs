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
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Maps <see cref="IWorkflowEngine{TKey,TState}" />'s generic lifecycle callbacks onto
///     <see cref="IWorkflowsRepository" />/<see cref="IEventBus" /> — the only place <see cref="WorkflowStatus" />
///     values get assigned. The enqueue (persist) and publish (event) side effects below are not atomic
///     with each other; that is an accepted application-level integration detail, not a guarantee
///     <c>SlideGenerator.Workflows</c> makes.
/// </summary>
internal sealed class GeneratorWorkflowObserver(
    IWorkflowsRepository workflowsRepository,
    IEventBus eventBus,
    ILogger<GeneratorWorkflowObserver> logger) : IWorkflowObserver<WorkflowKey, WorkflowSnapshot>
{
    /// <inheritdoc />
    public Task OnProgressAsync(WorkflowKey key, WorkflowSnapshot state, bool durable, CancellationToken ct)
    {
        return PersistAndPublishAsync(state with { WorkflowStatus = WorkflowStatus.Running }, durable, ct);
    }

    /// <inheritdoc />
    public Task OnPausedAsync(WorkflowKey key, WorkflowSnapshot state, CancellationToken ct)
    {
        return PersistAndPublishAsync(state with { WorkflowStatus = WorkflowStatus.Paused, Timestamp = DateTimeOffset.UtcNow },
            false, ct);
    }

    /// <inheritdoc />
    public Task OnResumedAsync(WorkflowKey key, WorkflowSnapshot state, CancellationToken ct)
    {
        return PersistAndPublishAsync(state with { WorkflowStatus = WorkflowStatus.Running, Timestamp = DateTimeOffset.UtcNow },
            false, ct);
    }

    /// <inheritdoc />
    public async Task OnTerminalAsync(WorkflowKey key, WorkflowTerminalResult<WorkflowSnapshot> result, CancellationToken ct)
    {
        var status = result.Outcome switch
        {
            WorkflowOutcome.Completed => WorkflowStatus.Complete,
            WorkflowOutcome.Cancelled => WorkflowStatus.Cancelled,
            WorkflowOutcome.Faulted => WorkflowStatus.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
        };
        if (result.Outcome == WorkflowOutcome.Faulted)
            logger.LogError(result.Exception, "Workflow {RequestId}/{WorkflowId} failed.", key.RequestId, key.WorkflowId);

        await PersistAndPublishAsync(result.State with { WorkflowStatus = status, Timestamp = DateTimeOffset.UtcNow },
            true, ct).ConfigureAwait(false);
        CleanupWorkflowTempFolder(key.RequestId, key.WorkflowId);
    }

    private async Task PersistAndPublishAsync(WorkflowSnapshot state, bool durable, CancellationToken ct)
    {
        workflowsRepository.Enqueue(state);
        if (durable) await workflowsRepository.FlushAsync(ct).ConfigureAwait(false);
        eventBus.Publish(state);
    }

    /// <summary>Deletes the per-workflow download folder after a terminal (non-Paused) outcome.</summary>
    private void CleanupWorkflowTempFolder(string requestId, int workflowId)
    {
        var dir = WorkflowTempFolder.GetPath(requestId, workflowId);
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up workflow temp folder '{Dir}'.", dir);
        }
    }
}