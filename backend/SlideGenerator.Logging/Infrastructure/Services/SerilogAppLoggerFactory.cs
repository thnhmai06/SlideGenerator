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
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Infrastructure.Formatting;
using SlideGenerator.Logging.Infrastructure.Options;

namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Creates file-backed application loggers using Serilog infrastructure.
/// </summary>
/// <param name="scopeManager">The scope manager shared by created loggers.</param>
/// <param name="options">The logging settings read from application configuration.</param>
internal sealed class SerilogAppLoggerFactory(IScopeManager scopeManager, LoggingOptions options) : IAppLoggerFactory
{
    /// <inheritdoc />
    public IAppLogger CreateLogger(string name, string logFilePath)
    {
        return CreateFileLogger(name, logFilePath, options.WorkflowMinimumLevel);
    }

    /// <inheritdoc />
    public IAppLogger CreateWorkflowLogger(string workflowId, string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        return CreateFileLogger("Workflow", logFilePath, options.WorkflowMinimumLevel)
            .WithScope($"Workflow/{workflowId}");
    }

    /// <summary>
    ///     Creates a Serilog file logger and wraps it behind the application logging abstraction.
    /// </summary>
    /// <param name="name">The logical logger name stored as a structured property.</param>
    /// <param name="logFilePath">The concrete file path where log events are written.</param>
    /// <param name="minimumLevel">The minimum event level accepted by the logger.</param>
    /// <returns>An application logger backed by the configured Serilog file sink.</returns>
    private IAppLogger CreateFileLogger(string name, string logFilePath, LogEventLevel minimumLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(logFilePath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var serilogLogger = new LoggerConfiguration()
            .ApplyMinimumLevel(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("LoggerName", name)
            .WriteTo.File(new LogFormatter(), logFilePath)
            .CreateLogger();

        return new SerilogAppLogger(serilogLogger, scopeManager);
    }
}