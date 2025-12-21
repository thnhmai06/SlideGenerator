namespace SlideGenerator.Domain.Job.Notifications;

/// <summary>
///     Represents a realtime job event.
/// </summary>
public sealed record JobEvent(
    string JobId,
    JobEventScope Scope,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    IReadOnlyDictionary<string, object?>? Data = null);

/// <summary>
///     Indicates which job scope the event belongs to.
/// </summary>
public enum JobEventScope
{
    Group,
    Sheet,
    System
}