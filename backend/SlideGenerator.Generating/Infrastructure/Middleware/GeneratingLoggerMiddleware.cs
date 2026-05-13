/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: GeneratingLoggerMiddleware.cs
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
using SlideGenerator.Generating.Domain.Models.Contexts;
using SlideGenerator.Logging.Domain.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generating.Infrastructure.Middleware;

/// <summary>
///     Ensures the workflow logger is initialized before each step executes,
///     supporting both first-run and persistence resume scenarios.
/// </summary>
public sealed class GeneratingLoggerMiddleware(IAppLoggerFactory loggerFactory) : IWorkflowStepMiddleware
{
    /// <inheritdoc />
    public async Task<ExecutionResult> HandleAsync(
        IStepExecutionContext context, IStepBody body, WorkflowStepDelegate next)
    {
        if (context.Workflow.Data is GeneratingContext { Logger: null } task)
            task.Logger = loggerFactory.CreateWorkflowLogger(task.WorkflowScope, task.WorkflowLogPath);

        return await next();
    }
}

