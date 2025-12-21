using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification for group status changes.
/// </summary>
public sealed record GroupStatusNotification(
    string GroupId,
    GroupStatus Status,
    string? Message,
    DateTimeOffset Timestamp);