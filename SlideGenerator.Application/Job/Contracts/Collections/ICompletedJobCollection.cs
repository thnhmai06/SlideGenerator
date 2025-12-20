using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Contract for managing completed jobs.
///     Completed jobs are typically in <c>Completed</c>, <c>Failed</c>, or <c>Cancelled</c> states.
/// </summary>
public interface ICompletedJobCollection : IJobCollection
{
    #region Remove Operations

    /// <summary>
    ///     Removes a group and all its sheets from the completed collection.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns><c>true</c> if removed; otherwise <c>false</c>.</returns>
    bool RemoveGroup(string groupId);

    /// <summary>
    ///     Removes a sheet from the completed collection.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    /// <returns><c>true</c> if removed; otherwise <c>false</c>.</returns>
    bool RemoveSheet(string sheetId);

    /// <summary>
    ///     Clears all completed groups and sheets.
    /// </summary>
    void ClearAll();

    #endregion

    #region Query by Status

    /// <summary>
    ///     Gets groups that finished successfully.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups();

    /// <summary>
    ///     Gets groups that finished with failures.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetFailedGroups();

    /// <summary>
    ///     Gets groups that were cancelled.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups();

    #endregion
}