using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SlideGenerator.Logging.Models;

namespace SlideGenerator.Logging.Sinks;

/// <summary>
///     A custom Serilog sink that buffers log events and persists them to the database in batches.
///     Uses scoped database contexts to ensure thread-safety and prevent memory leaks.
/// </summary>
/// <param name="scopeFactory">The factory used to create a new service scope for each batch insertion.</param>
public class WorkflowDatabaseSink(IServiceScopeFactory scopeFactory) : IBatchedLogEventSink
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

        var entries = new List<LogEntry>();

        foreach (var logEvent in batch)
        {
            logEvent.Properties.TryGetValue("TaskId", out var taskIdValue);
            logEvent.Properties.TryGetValue("Path", out var pathValue);

            var taskId = taskIdValue?.ToString().Trim('"') ?? "Unknown";
            var path = pathValue?.ToString().Trim('"') ?? "Global";

            ExceptionIdentifier? error = null;
            if (logEvent.Exception != null)
            {
                error = new ExceptionIdentifier(logEvent.Exception.GetType().Name, logEvent.Exception.Message);
            }
            // Handling destructured exception property if Serilog configuration passes it as a property
            else if (logEvent.Properties.TryGetValue("ExceptionDetail", out var exProp) && exProp is StructureValue sv)
            {
                var name = sv.Properties.FirstOrDefault(p => p.Name == "Name")?.Value.ToString().Trim('"') ?? "";
                var message = sv.Properties.FirstOrDefault(p => p.Name == "Message")?.Value.ToString().Trim('"') ?? "";
                error = new ExceptionIdentifier(name, message);
            }

            entries.Add(new LogEntry
            {
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Error = error,
                TaskId = taskId,
                Path = path
            });
        }

        if (entries.Count != 0)
        {
            dbContext.LogEntries.AddRange(entries);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    ///     Called when a batch interval expires but no events are pending.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}
