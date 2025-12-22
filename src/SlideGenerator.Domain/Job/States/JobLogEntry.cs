namespace SlideGenerator.Domain.Job.States;

/// <summary>
///     Persisted log entry for a sheet job.
/// </summary>
public sealed record JobLogEntry(
    string JobId,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    IReadOnlyDictionary<string, object?>? Data = null);