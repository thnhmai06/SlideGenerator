using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Slides.DTOs.Notifications;

/// <summary>
///     Notification for group status changes.
/// </summary>
public sealed record GroupStatusNotification(
    string GroupId,
    GroupStatus Status,
    string? Message,
    DateTimeOffset Timestamp);