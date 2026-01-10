using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs;

/// <summary>
///     Maps job statuses to job states for API responses.
/// </summary>
public static class JobStateMapper
{
    public static JobState ToJobState(this GroupStatus status)
    {
        return status switch
        {
            GroupStatus.Pending => JobState.Pending,
            GroupStatus.Running => JobState.Processing,
            GroupStatus.Paused => JobState.Paused,
            GroupStatus.Completed => JobState.Done,
            GroupStatus.Cancelled => JobState.Cancelled,
            GroupStatus.Failed => JobState.Error,
            _ => JobState.Error
        };
    }

    public static JobState ToJobState(this SheetJobStatus status)
    {
        return status switch
        {
            SheetJobStatus.Pending => JobState.Pending,
            SheetJobStatus.Running => JobState.Processing,
            SheetJobStatus.Paused => JobState.Paused,
            SheetJobStatus.Completed => JobState.Done,
            SheetJobStatus.Cancelled => JobState.Cancelled,
            SheetJobStatus.Failed => JobState.Error,
            _ => JobState.Error
        };
    }
}