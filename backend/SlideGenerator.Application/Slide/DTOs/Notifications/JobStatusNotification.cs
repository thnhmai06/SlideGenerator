using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;
using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification sent when job status changes.
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