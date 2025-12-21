namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification for sheet job progress updates.
/// </summary>
public sealed record JobProgressNotification(
    string JobId,
    int CurrentRow,
    int TotalRows,
    float Progress,
    int ErrorCount,
    DateTimeOffset Timestamp);