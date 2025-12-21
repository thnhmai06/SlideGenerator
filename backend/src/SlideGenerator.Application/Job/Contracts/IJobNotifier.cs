using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Notifications;

namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Sends realtime job notifications to subscribers.
/// </summary>
public interface IJobNotifier
{
    /// <summary>
    ///     Notifies subscribers of sheet progress updates.
    /// </summary>
    Task NotifyJobProgress(string jobId, int currentRow, int totalRows, float progress, int errorCount);

    /// <summary>
    ///     Notifies subscribers of sheet status changes.
    /// </summary>
    Task NotifyJobStatusChanged(string jobId, SheetJobStatus status, string? message = null);

    /// <summary>
    ///     Notifies subscribers of a sheet-level error.
    /// </summary>
    Task NotifyJobError(string jobId, string error);

    /// <summary>
    ///     Notifies subscribers of group progress updates.
    /// </summary>
    Task NotifyGroupProgress(string groupId, float progress, int errorCount);

    /// <summary>
    ///     Notifies subscribers of group status changes.
    /// </summary>
    Task NotifyGroupStatusChanged(string groupId, GroupStatus status, string? message = null);

    /// <summary>
    ///     Publishes a structured log event to subscribers.
    /// </summary>
    Task NotifyLog(JobEvent jobEvent);
}