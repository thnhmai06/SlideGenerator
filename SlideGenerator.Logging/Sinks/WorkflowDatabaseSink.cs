using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SlideGenerator.Logging.Models;

namespace SlideGenerator.Logging.Sinks;

public class WorkflowDatabaseSink(IServiceScopeFactory scopeFactory) : IBatchedLogEventSink
{
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

    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}
