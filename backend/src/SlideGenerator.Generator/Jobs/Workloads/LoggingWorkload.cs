/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: LoggingWorkload.cs
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
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Jobs.Engine;
using SlideGenerator.Logging.FileLogging;

namespace SlideGenerator.Generator.Jobs.Workloads;

/// <summary>
///     A per-job logger, threaded from <see cref="LoggingWorkload" /> (which owns the file-backed
///     <c>ILoggerFactory</c> for one job's run) down to <see cref="SlideGenerationWorkload" />, without the
///     generic <see cref="IJobContext{TState}" /> contract knowing anything about logging.
/// </summary>
internal interface IScopedLoggerContext
{
    ILogger Logger { get; }
}

/// <summary>
///     Pure decorator: opens a file-backed log scope for the duration of one job's run, then delegates
///     entirely to <paramref name="inner" />. Never calls <see cref="IJobContext{TState}.ReportAsync" />,
///     never publishes an event, never inspects the outcome — doing so would leak domain semantics into a
///     class that only exists to route log lines to the right file.
/// </summary>
internal sealed class LoggingWorkload(
    string logPath,
    IJobWorkload<JobSnapshot> inner,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier) : IJobWorkload<JobSnapshot>
{
    private static readonly string[] ScopePropertyNames = ["RequestId", "JobId", "RowIndex"];

    /// <inheritdoc />
    public async Task<JobSnapshot> RunAsync(JobSnapshot state, IJobContext<JobSnapshot> context, CancellationToken ct)
    {
        using var loggerFactory = fileLoggerFactory.CreateFile(logPath, ScopePropertyNames, notification =>
            logNotifier.Publish(new LogEntry
            {
                Timestamp = notification.Timestamp,
                Path = notification.Location,
                Level = ToLevelAbbreviation(notification.Level),
                Info = notification.Message
            }));
        var jobLogger = loggerFactory.CreateLogger(nameof(SlideGenerationWorkload));

        using var requestScope = LogContext.PushProperty("RequestId", state.RequestId);
        using var jobScope = LogContext.PushProperty("JobId", state.JobId);

        return await inner.RunAsync(state, new ScopedContext(context, jobLogger), ct).ConfigureAwait(false);
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
    private sealed class ScopedContext(IJobContext<JobSnapshot> inner, ILogger logger)
        : IJobContext<JobSnapshot>, IScopedLoggerContext
    {
        public bool IsResume => inner.IsResume;

        public Task ReportAsync(JobSnapshot state, bool durable = false, CancellationToken ct = default)
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