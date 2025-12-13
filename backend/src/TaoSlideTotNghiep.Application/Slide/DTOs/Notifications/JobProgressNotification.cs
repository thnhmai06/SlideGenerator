using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Notifications;

/// <summary>
/// Notification sent when job progress is updated.
/// </summary>
public record JobProgressNotification(JobProgressData Data)
    : Notification(SlideRequestType.JobStatus)
{
    // Convenience constructor
    public JobProgressNotification(string jobId, int currentRow, int totalRows, float progress)
        : this(new JobProgressData(jobId, currentRow, totalRows, progress))
    {
    }

    // Expose for backward compatibility
    public string JobId => Data.JobId;
    public int CurrentRow => Data.CurrentRow;
    public int TotalRows => Data.TotalRows;
    public float Progress => Data.Progress;
}