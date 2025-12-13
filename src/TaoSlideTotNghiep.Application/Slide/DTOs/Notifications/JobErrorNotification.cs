using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Notifications;

/// <summary>
/// Notification sent when a job encounters an error.
/// </summary>
public record JobErrorNotification(string JobId, string Error)
    : Notification(SlideRequestType.JobStatus);