/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingProgressMiddleware.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Application.Workflows;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Infrastructure.Middleware;

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
            ExtractData or CollectImage or EditImage => GeneratingPhase.PhaseB,
            ReplaceSlideData or CloseAllHandles => GeneratingPhase.PhaseC,
            _ => null
        };
    }
}