/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: LogManager.cs
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
using Microsoft.Extensions.Logging;
using SlideGenerator.Logging.Abstractions;

namespace SlideGenerator.Logging.Services;

/// <summary>
///     Provides services for managing and cleaning up workflow logs.
/// </summary>
public sealed class LogManager(ILogDbContext dbContext, ILogger<LogManager> logger)
{
    /// <summary>
    ///     Deletes the physical log file and the database record associated with the specified task ID.
    ///     This action is independent of the Job record itself.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task (workflow instance).</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    public async Task DeleteTaskLog(string taskId)
    {
        try
        {
            var entry = await dbContext.LogEntries
                .FirstOrDefaultAsync(l => l.TaskId == taskId)
                .ConfigureAwait(false);

            if (entry == null)
            {
                logger.LogWarning("No log entry found in database for TaskId: {TaskId}", taskId);
                return;
            }

            // 1. Delete physical file
            if (File.Exists(entry.LogFilePath))
            {
                try
                {
                    File.Delete(entry.LogFilePath);
                    logger.LogInformation("Deleted physical log file: {FilePath}", entry.LogFilePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete physical log file: {FilePath}", entry.LogFilePath);
                }
            }

            // 2. Delete database record
            dbContext.LogEntries.Remove(entry);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Deleted log entry from database for TaskId: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting task log for TaskId: {TaskId}", taskId);
            throw;
        }
    }
}