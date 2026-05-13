/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: LoggingOptionsReader.cs
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
using Serilog.Events;

namespace SlideGenerator.Logging.Infrastructure.Options;

/// <summary>
///     Reads logging infrastructure options from application configuration.
/// </summary>
internal static class LoggingOptionsReader
{
    /// <summary>
    ///     Builds logging options from configuration keys without reading any log file paths.
    /// </summary>
    /// <param name="configuration">The application configuration source.</param>
    /// <returns>The resolved logging options.</returns>
    public static LoggingOptions Read(IConfiguration configuration)
    {
        return new LoggingOptions
        {
            SystemMinimumLevel = ReadMinimumLevel(configuration, "Logging:System:MinimumLevel", LogEventLevel.Debug),
            WorkflowMinimumLevel = ReadMinimumLevel(configuration, "Logging:Workflow:MinimumLevel", LogEventLevel.Information)
        };
    }

    /// <summary>
    ///     Reads a Serilog event level from a concrete key with compatibility fallbacks.
    /// </summary>
    /// <param name="configuration">The application configuration source.</param>
    /// <param name="key">The preferred configuration key.</param>
    /// <param name="fallback">The level to use when the configured value is missing or invalid.</param>
    /// <returns>The resolved Serilog event level.</returns>
    private static LogEventLevel ReadMinimumLevel(
        IConfiguration configuration,
        string key,
        LogEventLevel fallback)
    {
        var configuredLevel =
            configuration[key] ??
            configuration["Logging:MinimumLevel"] ??
            configuration["Serilog:MinimumLevel:Default"] ??
            configuration["Serilog:MinimumLevel"];

        return Enum.TryParse<LogEventLevel>(configuredLevel, true, out var level)
            ? level
            : fallback;
    }
}


