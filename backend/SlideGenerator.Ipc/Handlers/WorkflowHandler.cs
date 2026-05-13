/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: WorkflowHandler.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */
using SlideGenerator.Generating.Application.Abstractions;
using SlideGenerator.Generating.Application.Workflows;
using SlideGenerator.Generating.Domain.Models;
using SlideGenerator.Generating.Domain.Models.Dto;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>workflow.*</c> JSON-RPC methods: start, cancel, pause, and resume.
///     Delegates execution to <see cref="IGeneratingService" /> and publishes lifecycle events
///     to <see cref="IGeneratingEventBus" /> for forwarding to the client as notifications.
/// </summary>
/// <remarks>
///     This handler is intentionally thin (<c>adapt-controller-thin</c>): it performs
///     no business logic — only parameter mapping and event publishing.
/// </remarks>
public sealed class WorkflowHandler(
    IGeneratingService generatingService,
    IGeneratingEventBus eventBus)
{
    /// <summary>
    ///     Starts a new slide generation workflow from the provided request and returns its instance ID.
    /// </summary>
    public async Task<string> StartAsync(GeneratingRequest request, CancellationToken ct)
    {
        var instanceId = await generatingService.StartAsync(request, ct).ConfigureAwait(false);
        Publish(instanceId, GeneratingEvent.WorkflowStarted, null, GeneratingStatus.Running);
        return instanceId;
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





