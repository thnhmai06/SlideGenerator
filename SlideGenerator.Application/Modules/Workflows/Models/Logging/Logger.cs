namespace SlideGenerator.Application.Modules.Workflows.Models.Logging;

/// <summary>
///     Provides thread-safe, sorted in-memory logging capabilities for a specific execution state.
/// </summary>
public sealed class Logger
{
    public static readonly HashSet<string> MsgFinished = ["[[COMPLETED]]", "[[FINISHED]]"];
    public static readonly HashSet<string> MsgPaused = ["[[PAUSED]]"];
    public static readonly HashSet<string> MsgCancelled = ["[[CANCELLED]]", "[[CANCELED]]"];
    
    private readonly Lock _lock = new();
    private readonly SortedSet<LogEntry> _logs = [];

    public static IReadOnlyCollection<LogEntry> Empty => [];

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

    /// <summary>Raised when a new log entry is added.</summary>
    public event Action<LogEntry>? OnLog;

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
    public void AddLog(LogLevel level, string message)
    {
        AddLog(new LogEntry(DateTimeOffset.Now, level, message));
    }
}