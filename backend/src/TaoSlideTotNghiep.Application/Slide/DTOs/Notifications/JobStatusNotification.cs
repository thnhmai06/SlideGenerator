using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Notifications;

/// <summary>
/// Notification sent when job status changes.
/// </summary>
public record JobStatusNotification(JobStatusData Data)
    : Notification(SlideRequestType.JobStatus)
{
    // Convenience constructor
    public JobStatusNotification(string jobId, SheetJobStatus status, string? message = null)
        : this(new JobStatusData(jobId, status, message))
    {
    }

    // Expose for backward compatibility
    public string JobId => Data.JobId;
    public SheetJobStatus Status => Data.Status;
    public string? Message => Data.Message;
}