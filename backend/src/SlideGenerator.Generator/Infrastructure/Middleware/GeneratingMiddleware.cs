/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingMiddleware.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Logging.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Infrastructure.Middleware;

/// <summary>
///     Ensures workflow-scoped services are initialized before each step executes,
///     supporting both first-run and persistence resume scenarios.
/// </summary>
internal sealed class GeneratingMiddleware(
    IFileLoggerFactory fileLoggerFactory,
    ICoordinatorFactory coordinatorFactory) : IWorkflowStepMiddleware
{
    /// <inheritdoc />
    public async Task<ExecutionResult> HandleAsync(
        IStepExecutionContext context, IStepBody body, WorkflowStepDelegate next)
    {
        if (context.Workflow.Data is not GeneratingContext data) return await next();

        data.LoggerFactory ??= fileLoggerFactory.CreateFile(
            data.WorkflowLogPath,
            $"Workflow/{data.WorkflowScope}");
        data.AssetCoordinator ??= coordinatorFactory.Create();

        return await next();
    }
}