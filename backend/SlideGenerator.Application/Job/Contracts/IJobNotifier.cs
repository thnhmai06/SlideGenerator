using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Publishes job and group updates to external subscribers (e.g., SignalR clients).
/// </summary>
public interface IJobNotifier
{
    /// <summary>
    ///     Publishes sheet progress information.
    /// </summary>
    /// <param name="jobId">The ID of the job.</param>
    /// <param name="currentRow">The zero-based index of the current row being processed.</param>
    /// <param name="totalRows">The total number of rows to be processed in the sheet.</param>
    /// <param name="progress">The progress percentage of the job, ranging from 0 to 100.</param>
    Task NotifyJobProgress(string jobId, int currentRow, int totalRows, float progress);

    /// <summary>
    ///     Publishes a sheet status change.
    /// </summary>
    /// <param name="jobId">The ID of the job.</param>
    /// <param name="status">The new status of the job.</param>
    /// <param name="message">An optional message providing additional information about the status change.</param>
    Task NotifyJobStatusChanged(string jobId, SheetJobStatus status, string? message = null);

    /// <summary>
    ///     Publishes a sheet error.
    /// </summary>
    /// <param name="jobId">The ID of the job.</param>
    /// <param name="error">The error message describing the nature of the error.</param>
    Task NotifyJobError(string jobId, string error);

    /// <summary>
    ///     Publishes group progress information.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="progress">The progress percentage of the group, ranging from 0 to 100.</param>
    Task NotifyGroupProgress(string groupId, float progress);

    /// <summary>
    ///     Publishes a group status change.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="status">The new status of the group.</param>
    /// <param name="message">An optional message providing additional information about the status change.</param>
    Task NotifyGroupStatusChanged(string groupId, GroupStatus status, string? message = null);
}