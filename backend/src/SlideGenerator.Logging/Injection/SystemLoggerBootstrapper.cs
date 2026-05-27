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
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using SlideGenerator.Logging.Infrastructure.Formatting;
using SlideGenerator.Logging.Infrastructure.Options;
using SlideGenerator.Logging.Infrastructure.Services;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using HardLink = SlideGenerator.Utilities.Helper.HardLink;

namespace SlideGenerator.Logging.Injection;

/// <summary>
///     Creates and owns the process-wide System logger at application startup.
/// </summary>
/// <remarks>
///     The System logger writes to a timestamped file inside the runtime log directory and to standard error.
///     It also refreshes <c>latest.log</c> as a hard link to the current System log file.
/// </remarks>
public static class SystemLoggerBootstrapper
{
    /// <summary>
    ///     Initializes the System logger inside the provided directory.
    /// </summary>
    /// <param name="systemLogDirectory">The directory that stores System log files.</param>
    /// <returns>The initialized System logger.</returns>
    public static ILogger Initialize(string systemLogDirectory)
    {
        return Initialize(systemLogDirectory, new LoggingOptions());
    }

    /// <summary>
    ///     Initializes the System logger from runtime path and application configuration.
    /// </summary>
    /// <param name="systemLogDirectory">The runtime directory that stores System log files.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The initialized System logger.</returns>
    public static ILogger Initialize(string systemLogDirectory, IConfiguration configuration)
    {
        return Initialize(systemLogDirectory, LoggingOptionsReader.Read(configuration));
    }

    /// <summary>
    ///     Initializes the System logger from the runtime path and already resolved logging options.
    /// </summary>
    /// <param name="systemLogDirectory">The runtime directory that stores System log files.</param>
    /// <param name="options">The resolved logging options.</param>
    /// <returns>The initialized System logger.</returns>
    internal static ILogger Initialize(string systemLogDirectory, LoggingOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemLogDirectory);

        Directory.CreateDirectory(systemLogDirectory);
        var currentLogFile = Path.Combine(systemLogDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        var fileFormatter = new LogFormatter();
        const string consoleTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{LoggerName}/{Scope}] {Level:u3}: {Message:lj}{NewLine}{Exception}";

        var serilogLogger = new LoggerConfiguration()
            .ApplyMinimumLevel(options.SystemMinimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("LoggerName", "System")
            .Enrich.WithProperty("Scope", "Global")
            .WriteTo.File(fileFormatter, currentLogFile)
            .WriteTo.Console(
                outputTemplate: consoleTemplate,
                theme: AnsiConsoleTheme.Code,
                standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        Log.Logger = serilogLogger;
        var logger = new SerilogLoggerFactory(serilogLogger, false).CreateLogger("System");
        CreateHardLink(systemLogDirectory, currentLogFile, logger);

        logger.LogInformation("System logger initialized. Current log file: {Path}", currentLogFile);

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
    ///     Recreates the <c>latest.log</c> hard link for the current System log file.
    /// </summary>
    /// <param name="systemLogDirectory">The directory containing System log files.</param>
    /// <param name="currentLogFile">The concrete timestamped log file for the current process run.</param>
    /// <param name="logger">The logger used to report hard-link creation failures.</param>
    private static void CreateHardLink(string systemLogDirectory, string currentLogFile, ILogger logger)
    {
        var latestPath = Path.Combine(systemLogDirectory, "latest.log");

        try
        {
            HardLink.Create(latestPath, currentLogFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Could not create latest.log hard link: {Message}", ex.Message);
        }
    }
}