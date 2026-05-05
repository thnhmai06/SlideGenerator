/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: LogEntry.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlideGenerator.Logging.Models;

/// <summary>
///     Represents a single log record persisted in the database.
///     Designed for high-performance retrieval and filtering by workflow task.
/// </summary>
public class LogEntry
{
    /// <summary>
    ///     Gets the unique primary key for the log entry.
    /// </summary>
    [Key]
    public int Id { get; init; }

    /// <summary>
    ///     Gets the precise time when the log event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Gets the severity level of the log (e.g., Information, Warning, Error).
    /// </summary>
    public string Level { get; init; } = null!;

    /// <summary>
    ///     Gets the formatted log message.
    /// </summary>
    public string Message { get; init; } = null!;

    /// <summary>
    ///     Gets a structured representation of the exception associated with the log, if any.
    ///     Stored as a JSONB object in the database for flexibility.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public ExceptionIdentifier? Error { get; init; }

    /// <summary>
    ///     Gets the unique identifier of the workflow task that generated this log.
    ///     This is the primary correlation key for reconstructing workflow history.
    /// </summary>
    public string TaskId { get; init; } = null!;

    /// <summary>
    ///     Gets the logical path or component name associated with the log entry.
    /// </summary>
    public string Path { get; init; } = null!;
}