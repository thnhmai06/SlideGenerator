/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Middleware.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Serilog.Events;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Logging.FileLogging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Middleware;

/// <summary>
///     Ensures workflow-scoped services are initialized before each step executes,
///     supporting both first-run and persistence resume scenarios.
/// </summary>
internal sealed class Middleware(IFileLoggerFactory fileLoggerFactory, ILogNotifier logNotifier) : IWorkflowStepMiddleware
{
    /// <summary>Ordered ambient property names that make up this module's Request/Job/Row log scope.</summary>
    private static readonly string[] ScopePropertyNames = ["RequestId", "JobId", "RowIndex"];

    /// <inheritdoc />
    public async Task<ExecutionResult> HandleAsync(
        IStepExecutionContext context, IStepBody body, WorkflowStepDelegate next)
    {
        if (context.Workflow.Data is not JobContext data) return await next();

        data.Transient.LoggerFactory ??= fileLoggerFactory.CreateFile(
            data.Persist.LogPath,
            ScopePropertyNames,
            notification => logNotifier.Publish(new LogEntry
            {
                Timestamp = notification.Timestamp,
                Path = notification.Location,
                Level = notification.Level switch
                {
                    LogEventLevel.Verbose => "VRB",
                    LogEventLevel.Debug => "DBG",
                    LogEventLevel.Information => "INF",
                    LogEventLevel.Warning => "WRN",
                    LogEventLevel.Error => "ERR",
                    LogEventLevel.Fatal => "FTL",
                    _ => "???"
                },
                Info = notification.Message
            }));

        return await next();
    }
}
