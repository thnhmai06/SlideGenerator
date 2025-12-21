namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification for realtime log messages.
/// </summary>
public sealed record JobLogNotification(
    string JobId,
    string Level,
    string Message,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?>? Data = null);