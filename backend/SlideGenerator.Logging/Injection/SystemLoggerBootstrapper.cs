/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SystemLoggerBootstrapper.cs
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
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Infrastructure.Formatting;
using SlideGenerator.Logging.Infrastructure.Options;
using SlideGenerator.Logging.Infrastructure.Services;

namespace SlideGenerator.Logging.Injection;

/// <summary>
///     Creates and owns the process-wide System logger at application startup.
/// </summary>
/// <remarks>
///     The System logger writes to a timestamped file inside the runtime log directory and to standard error.
///     It also refreshes <c>latest.log</c> as a symbolic link to the current System log file.
/// </remarks>
public static class SystemLoggerBootstrapper
{
    /// <summary>
    ///     Initializes the System logger inside the provided directory.
    /// </summary>
    /// <param name="systemLogDirectory">The directory that stores System log files.</param>
    /// <returns>The initialized System logger.</returns>
    public static ISystemLogger Initialize(string systemLogDirectory)
    {
        return Initialize(systemLogDirectory, new LoggingOptions());
    }

    /// <summary>
    ///     Initializes the System logger from runtime path and application configuration.
    /// </summary>
    /// <param name="systemLogDirectory">The runtime directory that stores System log files.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The initialized System logger.</returns>
    public static ISystemLogger Initialize(string systemLogDirectory, IConfiguration configuration)
    {
        return Initialize(systemLogDirectory, LoggingOptionsReader.Read(configuration));
    }

    /// <summary>
    ///     Initializes the System logger from runtime path and already resolved logging options.
    /// </summary>
    /// <param name="systemLogDirectory">The runtime directory that stores System log files.</param>
    /// <param name="options">The resolved logging options.</param>
    /// <returns>The initialized System logger.</returns>
    internal static ISystemLogger Initialize(string systemLogDirectory, LoggingOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemLogDirectory);

        Directory.CreateDirectory(systemLogDirectory);
        var currentLogFile = Path.Combine(systemLogDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        var formatter = new ScopedExceptionFormatter();

        var serilogLogger = new LoggerConfiguration()
            .ApplyMinimumLevel(options.SystemMinimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("LoggerName", "System")
            .WriteTo.File(formatter, currentLogFile)
            .WriteTo.Console(formatter, standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        Log.Logger = serilogLogger;
        var logger = new SerilogAppLogger(serilogLogger, new SerilogScopeManager());
        CreateLatestSymlink(systemLogDirectory, currentLogFile, logger);
        return logger;
    }

    /// <summary>
    ///     Flushes and closes the global Serilog logger synchronously.
    /// </summary>
    public static void Flush()
    {
        Log.CloseAndFlush();
    }

    /// <summary>
    ///     Flushes and closes the global Serilog logger asynchronously.
    /// </summary>
    /// <returns>A task that completes when pending log events have been flushed.</returns>
    public static async Task FlushAsync()
    {
        await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Recreates the <c>latest.log</c> symbolic link for the current System log file.
    /// </summary>
    /// <param name="systemLogDirectory">The directory containing System log files.</param>
    /// <param name="currentLogFile">The concrete timestamped log file for the current process run.</param>
    /// <param name="logger">The logger used to report symbolic-link creation failures.</param>
    private static void CreateLatestSymlink(string systemLogDirectory, string currentLogFile, IAppLogger logger)
    {
        var latestPath = Path.Combine(systemLogDirectory, "latest.log");

        try
        {
            if (File.Exists(latestPath)) File.Delete(latestPath);

            File.CreateSymbolicLink(latestPath, Path.GetFileName(currentLogFile));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            logger.Warning("Could not create latest.log symbolic link: {Message}", ex.Message);
        }
    }
}

