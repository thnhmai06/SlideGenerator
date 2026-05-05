/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: Registration.cs
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
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SlideGenerator.Logging.Sinks;

namespace SlideGenerator.Logging;

/// <summary>
///     Provides extension methods to register logging-related services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Custom theme for colored console output matching user preferences.
    /// </summary>
    private static readonly AnsiConsoleTheme CustomTheme = new(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\e[37m",
        [ConsoleThemeStyle.SecondaryText] = "\e[90m",
        [ConsoleThemeStyle.TertiaryText] = "\e[90m",
        [ConsoleThemeStyle.Invalid] = "\e[31m",
        [ConsoleThemeStyle.Null] = "\e[34m",
        [ConsoleThemeStyle.Name] = "\e[90m",
        [ConsoleThemeStyle.String] = "\e[36m",
        [ConsoleThemeStyle.Number] = "\e[35m",
        [ConsoleThemeStyle.Boolean] = "\e[34m",
        [ConsoleThemeStyle.Scalar] = "\e[32m",
        [ConsoleThemeStyle.LevelVerbose] = "\e[37m",
        [ConsoleThemeStyle.LevelDebug] = "\e[90m", // Gray
        [ConsoleThemeStyle.LevelInformation] = "\e[34m", // Blue
        [ConsoleThemeStyle.LevelWarning] = "\e[33m", // Yellow
        [ConsoleThemeStyle.LevelError] = "\e[31m", // Red
        [ConsoleThemeStyle.LevelFatal] = "\e[31m" // Red
    });

    /// <summary>
    ///     Configures the static Serilog logger using the provided configuration.
    /// </summary>
    /// <param name="configuration">The application configuration used to resolve Serilog settings.</param>
    /// <param name="logFilePath">The specific file path where logs should be written.</param>
    public static void ConfigureStaticLogger(IConfiguration configuration, string logFilePath)
    {
        if (!Directory.Exists(LoggingPaths.LogFolderPath)) Directory.CreateDirectory(LoggingPaths.LogFolderPath);

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            // 1. File Sink (Asynchronous)
            .WriteTo.Async(a => a.File(
                logFilePath,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] {Level:u3} [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                shared: true), 50000, true)
            // 2. Console Sink - Specifically using stderr to avoid polluting stdout (used for JSON-RPC)
            // This is how we tell the IDE/Debugger and Tauri that logs are on the error stream.
            .WriteTo.Console(
                theme: CustomTheme,
                standardErrorFromLevel: LogEventLevel.Verbose,
                outputTemplate: "[{Timestamp:HH:mm:ss}] {Level:u3} {Message:lj}{NewLine}{Exception}")
            // 3. Debug Sink - Specifically for IDE "Output" or "Debug" windows
            .WriteTo.Debug()
            .CreateLogger();

        Log.Logger = logger;
        Log.Information("Logging initialized. Log file: {LogFilePath}", logFilePath);
    }

    /// <param name="services">The service collection to add logging services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds system-centric Serilog logging backed by an asynchronous daily rolling file sink.
        /// </summary>
        /// <param name="configuration">The application configuration used to resolve Serilog settings.</param>
        /// <param name="logFilePath">The specific file path where logs should be written.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddSystemLogging(IConfiguration configuration,
            string logFilePath)
        {
            ConfigureStaticLogger(configuration, logFilePath);
            services.AddSerilog(Log.Logger, true);

            return services;
        }

        /// <summary>
        ///     Adds the custom database logging sink and associated infrastructure to the service collection.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddWorkflowLogging()
        {
            // Register the sink implementation so it can receive IServiceScopeFactory via DI
            services.AddSingleton<WorkflowDatabaseSink>();

            return services;
        }
    }
}