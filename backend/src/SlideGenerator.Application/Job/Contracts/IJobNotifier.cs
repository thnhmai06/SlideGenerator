using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Job.Contracts;

public interface IJobNotifier
{
    Task NotifyJobProgress(string jobId, int currentRow, int totalRows, float progress);
    Task NotifyJobStatusChanged(string jobId, SheetJobStatus status, string? message = null);
    Task NotifyJobError(string jobId, string error);
    Task NotifyGroupProgress(string groupId, float progress);
    Task NotifyGroupStatusChanged(string groupId, GroupStatus status, string? message = null);
}