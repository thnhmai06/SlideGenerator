/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: WorkflowDatabaseSink.cs
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

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SlideGenerator.Logging.Abstractions;
using SlideGenerator.Logging.Models;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Logging.Sinks;

/// <summary>
///     A custom Serilog sink that appends log events to task-specific files and
///     persists the file paths to the database.
/// </summary>
public sealed class WorkflowDatabaseSink(IServiceScopeFactory scopeFactory) : IBatchedLogEventSink
{
    private readonly ConcurrentDictionary<string, string> _taskLogPaths = new();

    /// <summary>
    ///     Processes a batch of log events, appends them to their respective task files,
    ///     and ensures a database record exists for each task's log file.
    /// </summary>
    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ILogDbContext>();
        var settingProvider = scope.ServiceProvider.GetRequiredService<ISettingProvider>();

        var groupedEvents = batch
            .Where(e => e.Properties.ContainsKey("TaskId"))
            .GroupBy(e => e.Properties["TaskId"].ToString().Trim('"'));

        foreach (var group in groupedEvents)
        {
            var taskId = group.Key;
            var logFilePath = await GetOrInitializeLogPath(taskId, group.First(), settingProvider, dbContext).ConfigureAwait(false);

            if (string.IsNullOrEmpty(logFilePath)) continue;

            await AppendLogsToFile(logFilePath, group).ConfigureAwait(false);
        }
    }

    private async Task<string> GetOrInitializeLogPath(
        string taskId,
        LogEvent firstEvent,
        ISettingProvider settingProvider,
        ILogDbContext dbContext)
    {
        if (_taskLogPaths.TryGetValue(taskId, out var path)) return path;

        // Try to find existing record in DB
        var existing = await dbContext.LogEntries
            .FirstOrDefaultAsync(l => l.TaskId == taskId)
            .ConfigureAwait(false);

        if (existing != null)
        {
            _taskLogPaths[taskId] = existing.LogFilePath;
            return existing.LogFilePath;
        }

        // Create new log file path
        firstEvent.Properties.TryGetValue("RecipeName", out var recipeNameValue);
        var recipeName = recipeNameValue?.ToString().Trim('"') ?? "UnknownRecipe";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        
        var tempFolder = settingProvider.Current.Download.Temp.FolderPath;
        var logDir = Path.Combine(tempFolder, "TaskLogs");
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

        var logFileName = $"{recipeName}_{timestamp}.log";
        var logFilePath = Path.Combine(logDir, logFileName);

        // Save to DB
        var entry = new LogEntry
        {
            TaskId = taskId,
            Timestamp = DateTimeOffset.UtcNow,
            LogFilePath = logFilePath
        };

        dbContext.LogEntries.Add(entry);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        _taskLogPaths[taskId] = logFilePath;
        return logFilePath;
    }

    private static async Task AppendLogsToFile(string path, IEnumerable<LogEvent> events)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var logEvent in events)
        {
            sb.AppendLine($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss}] {logEvent.Level:u3} {logEvent.RenderMessage()}");
            if (logEvent.Exception != null)
            {
                sb.AppendLine(logEvent.Exception.ToString());
            }
        }

        // Simple thread-safe append (for production, consider a more robust file-locking mechanism or a dedicated background writer)
        for (var i = 0; i < 5; i++)
        {
            try
            {
                await File.AppendAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
                break;
            }
            catch (IOException)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;
}