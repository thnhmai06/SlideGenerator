using SlideGenerator.Application.Features.Jobs.DTOs.Requests;
using SlideGenerator.Domain.Features.Jobs.Interfaces;

namespace SlideGenerator.Application.Features.Jobs.Contracts.Collections;

/// <summary>
///     Manages active jobs (pending/running/paused).
/// </summary>
public interface IActiveJobCollection : IJobCollection
{
    /// <summary>
    ///     Gets a value indicating whether any jobs are active.
    /// </summary>
    bool HasActiveJobs { get; }

    /// <summary>
    ///     Creates a new group job from the request.
    /// </summary>
    IJobGroup CreateGroup(JobCreate request);

    /// <summary>
    ///     Starts all sheet jobs in the group.
    /// </summary>
    void StartGroup(string groupId);

    /// <summary>
    ///     Requests pause for all running sheets in the group.
    /// </summary>
    void PauseGroup(string groupId);

    /// <summary>
    ///     Resumes all paused sheets in the group.
    /// </summary>
    void ResumeGroup(string groupId);

    /// <summary>
    ///     Cancels all active sheets in the group.
    /// </summary>
    void CancelGroup(string groupId);

    /// <summary>
    ///     Cancels and removes a group job and its persisted state.
    /// </summary>
    void CancelAndRemoveGroup(string groupId);

    /// <summary>
    ///     Requests pause for a single sheet.
    /// </summary>
    void PauseSheet(string sheetId);

    /// <summary>
    ///     Resumes a paused sheet.
    /// </summary>
    void ResumeSheet(string sheetId);

    /// <summary>
    ///     Cancels a sheet job.
    /// </summary>
    void CancelSheet(string sheetId);

    /// <summary>
    ///     Cancels and removes a sheet job and its persisted state.
    /// </summary>
    void CancelAndRemoveSheet(string sheetId);

    /// <summary>
    ///     Requests pause for all running groups.
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

    /// <summary>
    ///     Gets running groups.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetRunningGroups();

    /// <summary>
    ///     Gets paused groups.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetPausedGroups();

    /// <summary>
    ///     Gets pending groups.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetPendingGroups();

    /// <summary>
    ///     Gets a group by output folder path, if present.
    /// </summary>
    IJobGroup? GetGroupByOutputPath(string outputFolderPath);
}