/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: LoggingOptions.cs
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
using Serilog.Events;

namespace SlideGenerator.Logging.Infrastructure.Options;

/// <summary>
///     Contains Serilog runtime settings used by the infrastructure logger builders.
/// </summary>
internal sealed class LoggingOptions
{
    /// <summary>
    ///     Gets the minimum event level for developer-facing system logs.
    /// </summary>
    public LogEventLevel SystemMinimumLevel { get; init; } = LogEventLevel.Debug;

    /// <summary>
    ///     Gets the minimum event level for user-facing workflow logs.
    /// </summary>
    public LogEventLevel WorkflowMinimumLevel { get; init; } = LogEventLevel.Information;
}
