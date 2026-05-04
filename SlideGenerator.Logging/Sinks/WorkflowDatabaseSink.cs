using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SlideGenerator.Logging.Abstractions;
using SlideGenerator.Logging.Models;

namespace SlideGenerator.Logging.Sinks;

/// <summary>
///     A custom Serilog sink that buffers log events and persists them to the database in batches.
///     Uses scoped database contexts to ensure thread-safety and prevent memory leaks.
/// </summary>
/// <param name="scopeFactory">The factory used to create a new service scope for each batch insertion.</param>
public sealed class WorkflowDatabaseSink(IServiceScopeFactory scopeFactory) : IBatchedLogEventSink
{
    /// <summary>
    ///     Processes a batch of log events, transforms them into <see cref="LogEntry"/> entities,
    ///     and saves them to the database.
    /// </summary>
    /// <param name="batch">The collection of log events to persist.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ILogDbContext>();

        var entries = batch.Select(logEvent =>
        {
            logEvent.Properties.TryGetValue("TaskId", out var taskIdValue);
            logEvent.Properties.TryGetValue("Path", out var pathValue);

            var error = logEvent.Exception is { } ex
                ? new ExceptionIdentifier(ex.GetType().Name, ex.Message)
                : null;

            return new LogEntry
            {
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Error = error,
                TaskId = taskIdValue?.ToString().Trim('"') ?? "Unknown",
                Path = pathValue?.ToString().Trim('"') ?? "Global"
            };
        }).ToList();

        if (entries.Count != 0)
        {
            dbContext.LogEntries.AddRange(entries);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Called when a batch interval expires but no events are pending.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnEmptyBatchAsync() => Task.CompletedTask;
}
