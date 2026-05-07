/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: ILogDbContext.cs
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

using Microsoft.EntityFrameworkCore;
using SlideGenerator.Logging.Models;

namespace SlideGenerator.Logging.Abstractions;

/// <summary>
///     Defines the abstraction for the database context used to persist log entries.
///     This interface allows the logging sink to interact with different database providers
///     while maintaining a consistent data model.
/// </summary>
public interface ILogDbContext
{
    /// <summary>
    ///     Gets the collection of log entries stored in the database.
    /// </summary>
    DbSet<LogEntry> LogEntries { get; }

    /// <summary>
    ///     Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous save operation. The task result contains the number of state entries
    ///     written to the database.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}