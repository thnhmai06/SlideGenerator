using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Contract for managing active jobs.
///     Active jobs are typically in <c>Pending</c>, <c>Running</c>, or <c>Paused</c> states.
/// </summary>
public interface IActiveJobCollection : IJobCollection
{
    #region Group Lifecycle

    /// <summary>
    ///     Creates a new job group and its sheets in the active collection.
    /// </summary>
    /// <param name="request">The group creation request.</param>
    /// <returns>The created group.</returns>
    IJobGroup CreateGroup(GenerateSlideGroupCreate request);

    /// <summary>
    ///     Starts processing all pending sheets in a group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    void StartGroup(string groupId);

    /// <summary>
    ///     Pauses all running sheets in a group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    void PauseGroup(string groupId);

    /// <summary>
    ///     Resumes all paused sheets in a group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    void ResumeGroup(string groupId);

    /// <summary>
    ///     Cancels all active sheets in a group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    void CancelGroup(string groupId);

    #endregion

    #region Sheet Lifecycle

    /// <summary>
    ///     Pauses an active sheet.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    void PauseSheet(string sheetId);

    /// <summary>
    ///     Resumes a paused sheet.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    void ResumeSheet(string sheetId);

    /// <summary>
    ///     Cancels an active sheet.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    void CancelSheet(string sheetId);

    #endregion

    #region Bulk Operations

    /// <summary>
    ///     Pauses all running groups.
    /// </summary>
    void PauseAll();

    /// <summary>
    ///     Resumes all paused groups.
    /// </summary>
    void ResumeAll();

    /// <summary>
    ///     Cancels all active groups.
    /// </summary>
    void CancelAll();

    #endregion

    #region Query

    /// <summary>
    ///     Gets whether this collection currently contains any active jobs.
    /// </summary>
    bool HasActiveJobs { get; }

    /// <summary>
    ///     Gets groups that are currently running.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetRunningGroups();

    /// <summary>
    ///     Gets groups that are currently paused.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetPausedGroups();

    /// <summary>
    ///     Gets groups that are pending (created but not started).
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetPendingGroups();

    #endregion
}