using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification for sheet job status changes.
/// </summary>
public sealed record JobStatusNotification(
    string JobId,
    SheetJobStatus Status,
    string? Message,
    DateTimeOffset Timestamp);