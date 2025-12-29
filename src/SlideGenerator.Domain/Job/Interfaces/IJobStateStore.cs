using SlideGenerator.Domain.Job.States;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Persists and restores job state for resume.
/// </summary>
public interface IJobStateStore
{
    /// <summary>
    ///     Persists group state.
    /// </summary>
    Task SaveGroupAsync(GroupJobState state, CancellationToken cancellationToken);

    /// <summary>
    ///     Persists sheet state.
    /// </summary>
    Task SaveSheetAsync(SheetJobState state, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a group state by id.
    /// </summary>
    Task<GroupJobState?> GetGroupAsync(string groupId, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a sheet state by id.
    /// </summary>
    Task<SheetJobState?> GetSheetAsync(string sheetId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets active group states.
    /// </summary>
    Task<IReadOnlyList<GroupJobState>> GetActiveGroupsAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Gets all group states (active + completed).
    /// </summary>
    Task<IReadOnlyList<GroupJobState>> GetAllGroupsAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Appends a log entry for a job.
    /// </summary>
    Task AppendJobLogAsync(JobLogEntry entry, CancellationToken cancellationToken);

    /// <summary>
    ///     Appends multiple log entries for a job.
    /// </summary>
    Task AppendJobLogsAsync(IReadOnlyCollection<JobLogEntry> entries, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets all log entries for a job.
    /// </summary>
    Task<IReadOnlyList<JobLogEntry>> GetJobLogsAsync(string jobId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets sheet states for a group.
    /// </summary>
    Task<IReadOnlyList<SheetJobState>> GetSheetsByGroupAsync(string groupId, CancellationToken cancellationToken);

    /// <summary>
    ///     Removes a group state and its sheets.
    /// </summary>
    Task RemoveGroupAsync(string groupId, CancellationToken cancellationToken);

    /// <summary>
    ///     Removes a sheet state.
    /// </summary>
    Task RemoveSheetAsync(string sheetId, CancellationToken cancellationToken);
}