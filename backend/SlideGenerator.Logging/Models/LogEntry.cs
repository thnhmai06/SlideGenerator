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
///     Represents a single log record persisted in the database, pointing to a physical log file.
/// </summary>
public class LogEntry
{
    /// <summary>
    ///     Gets the unique primary key for the log entry.
    /// </summary>
    [Key]
    public int Id { get; init; }

    /// <summary>
    ///     Gets the unique identifier of the workflow task (Job) that generated this log.
    /// </summary>
    public string TaskId { get; init; } = null!;

    /// <summary>
    ///     Gets the start time of the job.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Gets the absolute path to the physical log file.
    /// </summary>
    public string LogFilePath { get; init; } = null!;
}