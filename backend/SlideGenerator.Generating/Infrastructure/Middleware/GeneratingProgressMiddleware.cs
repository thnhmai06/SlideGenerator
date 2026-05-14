/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: GeneratingProgressMiddleware.cs
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
using SlideGenerator.Generating.Application.Steps;
using SlideGenerator.Generating.Application.Workflows;
using SlideGenerator.Generating.Domain.Models;
using SlideGenerator.Generating.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generating.Infrastructure.Middleware;

/// <summary>
///     Publishes a <see cref="GeneratingEvent.StepCompleted" /> progress notification
///     after each workflow step executes, including the resolved <see cref="GeneratingPhase" />.
/// </summary>
internal sealed class GeneratingProgressMiddleware(IGeneratingEventBus eventBus) : IWorkflowStepMiddleware
{
    /// <inheritdoc />
    public async Task<ExecutionResult> HandleAsync(
        IStepExecutionContext context, IStepBody body, WorkflowStepDelegate next)
    {
        var result = await next();

        if (context.Workflow.Data is GeneratingContext)
            eventBus.Publish(new GeneratingProgress
            {
                WorkflowInstanceId = context.Workflow.Id,
                Event = GeneratingEvent.StepCompleted,
                StepName = body.GetType().Name,
                Phase = ResolvePhase(body),
                Status = GeneratingStatus.Running,
                Timestamp = DateTimeOffset.UtcNow
            });

        return result;
    }

    private static GeneratingPhase? ResolvePhase(IStepBody body)
    {
        return body switch
        {
            ValidateRequest or CreateTemplate => GeneratingPhase.PhaseA,
            ExtractData or DownloadImage or EditImage => GeneratingPhase.PhaseB,
            ReplaceSlideData or CloseAllHandles => GeneratingPhase.PhaseC,
            _ => null
        };
    }
}