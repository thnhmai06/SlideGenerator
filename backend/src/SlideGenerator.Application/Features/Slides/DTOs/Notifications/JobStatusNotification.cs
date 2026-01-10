using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Slides.DTOs.Notifications;

/// <summary>
///     Notification for sheet job status changes.
/// </summary>
public sealed record JobStatusNotification(
    string JobId,
    SheetJobStatus Status,
    string? Message,
    DateTimeOffset Timestamp);