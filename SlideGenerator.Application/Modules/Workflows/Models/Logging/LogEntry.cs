namespace SlideGenerator.Application.Modules.Workflows.Models.Logging;

/// <summary>
///     Represents a single log entry recorded during workflow execution.
/// </summary>
/// <param name="Timestamp">The time the log was recorded.</param>
/// <param name="Level">The severity level of the log.</param>
/// <param name="Message">The log message.</param>
public readonly record struct LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Message) 
    : IComparable<LogEntry>
{
    /// <inheritdoc />
    public int CompareTo(LogEntry other)
    {
        var timestampComparison = Timestamp.CompareTo(other.Timestamp);
        if (timestampComparison != 0) return timestampComparison;
        
        var levelComparison = Level.CompareTo(other.Level);
        if (levelComparison != 0) return levelComparison;

        return string.Compare(Message, other.Message, StringComparison.Ordinal);
    }
}
