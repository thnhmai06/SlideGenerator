/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: LoggingWorkflowRunner.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Events;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Logging.FileLogging;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     A per-workflow logger, threaded from <see cref="LoggingWorkflowRunner" /> (which owns the file-backed
///     <c>ILoggerFactory</c> for one workflow's run) down to <see cref="SlideGenerationWorkflow" />, without the
///     generic <see cref="IWorkflowContext{TState}" /> contract knowing anything about logging.
/// </summary>
internal interface IScopedLoggerContext
{
    ILogger Logger { get; }
}

/// <summary>
///     Pure decorator: opens one file-backed log scope for the duration of a workflow's whole run — shared by
///     every job it runs concurrently, so there is exactly one sink per log file — then delegates entirely to
///     <paramref name="inner" />. Never calls <see cref="IWorkflowContext{TState}.ReportAsync" />, never publishes
///     an event, never inspects the outcome.
/// </summary>
internal sealed class LoggingWorkflowRunner(
    string logPath,
    IWorkflowRunner<WorkflowSnapshot> inner,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier) : IWorkflowRunner<WorkflowSnapshot>
{
    private static readonly string[] ScopePropertyNames = ["WorkflowId", "JobId", "RowIndex"];

    /// <inheritdoc />
    public async Task<WorkflowSnapshot> RunAsync(
        WorkflowSnapshot state, IWorkflowContext<WorkflowSnapshot> context, CancellationToken ct)
    {
        using var loggerFactory = fileLoggerFactory.CreateFile(logPath, ScopePropertyNames, notification =>
            logNotifier.Publish(new LogEntry
            {
                Timestamp = notification.Timestamp,
                Path = notification.Location,
                Level = ToLevelAbbreviation(notification.Level),
                Info = notification.Message
            }));
        var workflowLogger = loggerFactory.CreateLogger(nameof(SlideGenerationWorkflow));

        using var workflowScope = LogContext.PushProperty("WorkflowId", state.WorkflowId);

        return await inner.RunAsync(state, new ScopedContext(context, workflowLogger), ct).ConfigureAwait(false);
    }

    private static string ToLevelAbbreviation(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => "???"
        };
    }

    /// <summary>Forwards everything to <paramref name="inner" /> unchanged, only adding <see cref="Logger" />.</summary>
    private sealed class ScopedContext(IWorkflowContext<WorkflowSnapshot> inner, ILogger logger)
        : IWorkflowContext<WorkflowSnapshot>, IScopedLoggerContext
    {
        public bool IsResume => inner.IsResume;

        public Task ReportAsync(WorkflowSnapshot state, bool durable = false, CancellationToken ct = default)
        {
            return inner.ReportAsync(state, durable, ct);
        }

        public Task CheckpointAsync(CancellationToken ct)
        {
            return inner.CheckpointAsync(ct);
        }

        public ILogger Logger { get; } = logger;
    }
}
