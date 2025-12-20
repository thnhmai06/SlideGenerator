using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Entry point for job management.
///     Exposes two separate collections: active jobs and completed jobs.
/// </summary>
public interface IJobManager
{
    #region Collections

    /// <summary>
    ///     Gets the collection of active jobs (pending/running/paused).
    /// </summary>
    IActiveJobCollection Active { get; }

    /// <summary>
    ///     Gets the collection of completed jobs (completed/failed/cancelled).
    /// </summary>
    ICompletedJobCollection Completed { get; }

    #endregion

    #region Cross-Collection Query

    /// <summary>
    ///     Finds a group by id in either active or completed collections.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The group if found; otherwise <c>null</c>.</returns>
    IJobGroup? GetGroup(string groupId);

    /// <summary>
    ///     Finds a sheet by id in either active or completed collections.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    /// <returns>The sheet if found; otherwise <c>null</c>.</returns>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Gets all groups from both collections.
    /// </summary>
    /// <returns>A dictionary of group id to group.</returns>
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();

    #endregion
}