/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: GeneratingService.cs
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
using SlideGenerator.Generating.Domain.Models;
using SlideGenerator.Generating.Domain.Models.Contexts;
using SlideGenerator.Generating.Domain.Models.Dto;
using SlideGenerator.Generating.Application.Workflows;
using SlideGenerator.Logging.Domain.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace SlideGenerator.Generating.Infrastructure.Services;

/// <summary>
///     Implements <see cref="IGeneratingService" /> by wrapping the WorkflowCore
///     <see cref="IWorkflowHost" /> and <see cref="IWorkflowController" />.
///     Lives in Infrastructure because it has a direct dependency on the WorkflowCore framework.
/// </summary>
public sealed class GeneratingService(
    IWorkflowHost workflowHost,
    IWorkflowController workflowController,
    IGeneratingEventBus eventBus,
    ISystemLogger logger)
    : IGeneratingService
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        workflowHost.RegisterWorkflow<GeneratingWorkflow, GeneratingContext>();

        workflowHost.OnLifeCycleEvent += e =>
        {
            switch (e)
            {
                case WorkflowCompleted wc:
                    eventBus.Publish(new GeneratingProgress
                    {
                        WorkflowInstanceId = wc.WorkflowInstanceId,
                        Event = GeneratingEvent.WorkflowCompleted,
                        Status = GeneratingStatus.Complete,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                    break;
                case WorkflowError we:
                    eventBus.Publish(new GeneratingProgress
                    {
                        WorkflowInstanceId = we.WorkflowInstanceId,
                        Event = GeneratingEvent.WorkflowError,
                        Status = GeneratingStatus.Error,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                    break;
            }
        };

        await workflowHost.StartAsync(ct).ConfigureAwait(false);
        logger.Information("WorkflowCore host started and GeneratingWorkflow registered.");
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        await workflowHost.StopAsync(ct).ConfigureAwait(false);
        logger.Information("WorkflowCore host stopped.");
    }

    /// <inheritdoc />
    public async Task<string> StartAsync(GeneratingRequest request, CancellationToken ct = default)
    {
        var context = new GeneratingContext
        {
            Request = request,
            WorkflowLogPath = ResolveWorkflowLogPath(request),
            WorkflowScope = ResolveWorkflowScope(request)
        };
        var instanceId = await workflowHost
            .StartWorkflow(nameof(GeneratingWorkflow), 1, context)
            .ConfigureAwait(false);

        logger.Information("Started workflow {InstanceId} for request.", instanceId);
        return instanceId;
    }

    /// <inheritdoc />
    public async Task<bool> CancelAsync(string instanceId, CancellationToken ct = default)
    {
        var success = await workflowController
            .TerminateWorkflow(instanceId)
            .ConfigureAwait(false);

        if (success)
            logger.Information("Cancelled workflow {InstanceId}.", instanceId);
        else
            logger.Warning("Failed to cancel workflow {InstanceId} - may not be running.", instanceId);

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> PauseAsync(string instanceId, CancellationToken ct = default)
    {
        var success = await workflowController
            .SuspendWorkflow(instanceId)
            .ConfigureAwait(false);

        if (success)
            logger.Information("Paused workflow {InstanceId}.", instanceId);
        else
            logger.Warning("Failed to pause workflow {InstanceId}.", instanceId);

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> ResumeAsync(string instanceId, CancellationToken ct = default)
    {
        var success = await workflowController
            .ResumeWorkflow(instanceId)
            .ConfigureAwait(false);

        if (success)
            logger.Information("Resumed workflow {InstanceId}.", instanceId);
        else
            logger.Warning("Failed to resume workflow {InstanceId}.", instanceId);

        return success;
    }

    private static string ResolveWorkflowLogPath(GeneratingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.WorkflowLogFilePath)) return request.WorkflowLogFilePath;

        var fileName = string.Join("_", request.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "workflow";
        return Path.Combine(request.SaveFolder, $"{fileName}.log");
    }

    private static string ResolveWorkflowScope(GeneratingRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Name) ? "Workflow" : request.Name;
    }
}






