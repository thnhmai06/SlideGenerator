/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SerilogConfiguration.cs
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

namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Provides Serilog configuration helpers used by logger builders.
/// </summary>
internal static class SerilogConfiguration
{
    /// <summary>
    ///     Applies a concrete minimum event level to a Serilog logger configuration.
    /// </summary>
    /// <param name="configuration">The logger configuration to update.</param>
    /// <param name="minimumLevel">The minimum level accepted by the logger.</param>
    /// <returns>The updated logger configuration.</returns>
    public static LoggerConfiguration ApplyMinimumLevel(
        this LoggerConfiguration configuration,
        LogEventLevel minimumLevel)
    {
        return minimumLevel switch
        {
            LogEventLevel.Verbose => configuration.MinimumLevel.Verbose(),
            LogEventLevel.Debug => configuration.MinimumLevel.Debug(),
            LogEventLevel.Information => configuration.MinimumLevel.Information(),
            LogEventLevel.Warning => configuration.MinimumLevel.Warning(),
            LogEventLevel.Error => configuration.MinimumLevel.Error(),
            LogEventLevel.Fatal => configuration.MinimumLevel.Fatal(),
            _ => configuration.MinimumLevel.Information()
        };
    }
}


