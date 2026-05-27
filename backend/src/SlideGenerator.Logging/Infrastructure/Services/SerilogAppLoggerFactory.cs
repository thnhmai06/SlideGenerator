/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SerilogAppLoggerFactory.cs
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

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Infrastructure.Formatting;
using SlideGenerator.Logging.Infrastructure.Options;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Creates file-backed application loggers using Serilog infrastructure,
///     returning standard MEL <see cref="ILogger" /> instances.
/// </summary>
internal sealed class SerilogAppLoggerFactory(LoggingOptions options) : IAppLoggerFactory
{
    /// <inheritdoc />
    public ILogger CreateLogger(string name, string logFilePath)
    {
        return CreateFileLogger(name, logFilePath, options.WorkflowMinimumLevel);
    }

    /// <inheritdoc />
    public ILogger CreateWorkflowLogger(string workflowId, string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        return CreateFileLogger("Workflow", logFilePath, options.WorkflowMinimumLevel, $"Workflow/{workflowId}");
    }

    private static ILogger CreateFileLogger(
        string name, string logFilePath, LogEventLevel minimumLevel, string? scope = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(logFilePath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var config = new LoggerConfiguration()
            .ApplyMinimumLevel(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("LoggerName", name);

        if (scope != null)
            config = config.Enrich.WithProperty("Scope", scope);

        var serilogLogger = config
            .WriteTo.File(new LogFormatter(), logFilePath)
            .CreateLogger();

        return new SerilogLoggerFactory(serilogLogger, true).CreateLogger(name);
    }
}