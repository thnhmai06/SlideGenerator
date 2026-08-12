/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: LogNotification.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Serilog.Events;

namespace SlideGenerator.Logging.FileLogging;

/// <summary>
///     A single log event, captured at the point of writing, carrying the Request/Job/Row scope path it
///     was written under. Passed to <c>IFileLoggerFactory.CreateFile</c>'s <c>onLogEvent</c> callback so a
///     caller can forward it to the frontend in real time, without this module depending upward on any
///     consumer-specific type.
/// </summary>
public sealed record LogNotification
{
    /// <summary>Gets the timestamp when this event was written.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Gets the scope path this event belongs to: <c>"&lt;requestId&gt;"</c>,
    ///     <c>"&lt;requestId&gt;/&lt;jobId&gt;"</c>, or <c>"&lt;requestId&gt;/&lt;jobId&gt;/&lt;rowIndex&gt;"</c>. TODO: fix comment
    /// </summary>
    public required string Location { get; init; }

    /// <summary>Gets the log level this event was written at.</summary>
    public required LogEventLevel Level { get; init; }

    /// <summary>Gets the rendered log message.</summary>
    public required string Message { get; init; }
}
