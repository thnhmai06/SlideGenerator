namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification for sheet job errors.
/// </summary>
public sealed record JobErrorNotification(
    string JobId,
    string Error,
    DateTimeOffset Timestamp);