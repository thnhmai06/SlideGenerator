using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Manages completed jobs (finished/failed/cancelled).
/// </summary>
public interface ICompletedJobCollection : IJobCollection
{
    /// <summary>
    ///     Removes a completed group by id.
    /// </summary>
    bool RemoveGroup(string groupId);

    /// <summary>
    ///     Removes a completed sheet by id.
    /// </summary>
    bool RemoveSheet(string sheetId);

    /// <summary>
    ///     Clears all completed jobs.
    /// </summary>
    void ClearAll();

    /// <summary>
    ///     Gets groups that completed successfully.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups();

    /// <summary>
    ///     Gets groups that failed.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetFailedGroups();

    /// <summary>
    ///     Gets groups that were cancelled.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups();
}