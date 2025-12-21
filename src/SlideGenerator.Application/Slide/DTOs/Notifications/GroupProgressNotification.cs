namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification for group progress updates.
/// </summary>
public sealed record GroupProgressNotification(
    string GroupId,
    float Progress,
    int ErrorCount,
    DateTimeOffset Timestamp);