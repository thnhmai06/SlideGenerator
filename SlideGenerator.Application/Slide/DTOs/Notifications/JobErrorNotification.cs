using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
/// Notification sent when a job encounters an error.
/// </summary>
public record JobErrorNotification(string JobId, string Error)
    : Notification(SlideRequestType.JobStatus);