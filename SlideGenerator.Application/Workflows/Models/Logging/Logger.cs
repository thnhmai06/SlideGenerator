namespace SlideGenerator.Application.Workflows.Models.Logging;

/// <summary>
///     Provides thread-safe, sorted in-memory logging capabilities for a specific execution state.
/// </summary>
public sealed class Logger
{
    private readonly SortedSet<LogEntry> _logs = [];
    private readonly Lock _lock = new();
    
    public static IReadOnlyCollection<LogEntry> Empty => [];

    /// <summary>Raised when a new log entry is added.</summary>
    public event Action<LogEntry>? OnLog;

    /// <summary>Gets a snapshot of all log entries, sorted by timestamp and severity.</summary>
    public IReadOnlyCollection<LogEntry> Logs
    {
        get
        {
            lock (_lock)
            {
                return _logs.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>Adds a new log entry to the logger in a thread-safe manner.</summary>
    /// <param name="log">The log entry to add.</param>
    public void AddLog(LogEntry log)
    {
        lock (_lock)
        {
            _logs.Add(log);
        }
        OnLog?.Invoke(log);
    }

    /// <summary>Adds a new log entry with the current timestamp in a thread-safe manner.</summary>
    /// <param name="level">The severity level.</param>
    /// <param name="message">The log message.</param>
    public void AddLog(LogLevel level, string message) => 
        AddLog(new LogEntry(DateTimeOffset.Now, level, message));
}
