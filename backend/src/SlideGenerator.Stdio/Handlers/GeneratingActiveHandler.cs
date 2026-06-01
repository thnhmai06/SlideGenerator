/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: GeneratingActiveHandler.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Workflows;
using SlideGenerator.Generator.Domain.Models;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>generating.active.*</c> JSON-RPC methods: start, cancel, pause, resume, list, and query.
///     Delegates execution to <see cref="IGeneratingService" /> and publishes lifecycle events
///     to <see cref="IGeneratingEventBus" /> for forwarding to the client as notifications.
/// </summary>
/// <remarks>
///     This handler is intentionally thin: it performs no business logic — only parameter mapping
///     and event publishing.
/// </remarks>
public sealed class GeneratingActiveHandler(
    IGeneratingService generatingService,
    IGeneratingEventBus eventBus)
{
    /// <summary>
    ///     Starts a new slide generation workflow from the provided request and returns its instance ID.
    ///     The <c>WorkflowStarted</c> event is published by <see cref="IGeneratingService" /> via the lifecycle handler.
    /// </summary>
    public Task<string> StartAsync(GeneratingRequest request, CancellationToken ct)
    {
        return generatingService.StartAsync(request, ct);
    }

    /// <summary>
    ///     Terminates a running workflow instance.
    /// </summary>
    public async Task<bool> CancelAsync(string workflowInstanceId, CancellationToken ct)
    {
        var success = await generatingService.CancelAsync(workflowInstanceId, ct).ConfigureAwait(false);
        if (success) Publish(workflowInstanceId, GeneratingEvent.WorkflowCancelled, null, GeneratingStatus.Cancelled);
        return success;
    }

    /// <summary>
    ///     Suspends a running workflow instance.
    /// </summary>
    public async Task<bool> PauseAsync(string workflowInstanceId, CancellationToken ct)
    {
        var success = await generatingService.PauseAsync(workflowInstanceId, ct).ConfigureAwait(false);
        if (success) Publish(workflowInstanceId, GeneratingEvent.WorkflowSuspended, null, GeneratingStatus.Paused);
        return success;
    }

    /// <summary>
    ///     Resumes a previously suspended workflow instance.
    /// </summary>
    public async Task<bool> ResumeAsync(string workflowInstanceId, CancellationToken ct)
    {
        var success = await generatingService.ResumeAsync(workflowInstanceId, ct).ConfigureAwait(false);
        if (success) Publish(workflowInstanceId, GeneratingEvent.WorkflowResumed, null, GeneratingStatus.Running);
        return success;
    }

    /// <summary>
    ///     Cancels all currently active (running and paused) workflow instances.
    /// </summary>
    /// <returns>The number of instances successfully cancelled.</returns>
    public async Task<int> CancelAllAsync(CancellationToken ct)
    {
        var instances = await generatingService.ListActiveAsync(ct).ConfigureAwait(false);
        var count = 0;
        foreach (var instance in instances)
        {
            var success = await generatingService.CancelAsync(instance.InstanceId, ct).ConfigureAwait(false);
            if (!success) continue;
            Publish(instance.InstanceId, GeneratingEvent.WorkflowCancelled, null, GeneratingStatus.Cancelled);
            count++;
        }

        return count;
    }

    /// <summary>
    ///     Pauses all currently running (non-paused) workflow instances.
    /// </summary>
    /// <returns>The number of instances successfully paused.</returns>
    public async Task<int> PauseAllAsync(CancellationToken ct)
    {
        var instances = await generatingService.ListActiveAsync(ct).ConfigureAwait(false);
        var count = 0;
        foreach (var instance in instances.Where(i => i.Status == GeneratingStatus.Running))
        {
            var success = await generatingService.PauseAsync(instance.InstanceId, ct).ConfigureAwait(false);
            if (!success) continue;
            Publish(instance.InstanceId, GeneratingEvent.WorkflowSuspended, null, GeneratingStatus.Paused);
            count++;
        }

        return count;
    }

    /// <summary>
    ///     Returns summaries of all currently active (running or paused) workflow instances.
    /// </summary>
    public Task<IReadOnlyList<GeneratingSummary>> ListAsync(CancellationToken ct)
    {
        return generatingService.ListActiveAsync(ct);
    }

    /// <summary>
    ///     Returns the summary of a specific active workflow instance, or <see langword="null" /> if not found.
    /// </summary>
    public Task<GeneratingSummary?> QueryAsync(string workflowInstanceId, CancellationToken ct)
    {
        return generatingService.QueryAsync(workflowInstanceId, ct);
    }

    private void Publish(string instanceId, GeneratingEvent evt, GeneratingPhase? phase, GeneratingStatus status)
    {
        eventBus.Publish(new GeneratingProgress
        {
            WorkflowInstanceId = instanceId,
            Event = evt,
            Phase = phase,
            Status = status,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}