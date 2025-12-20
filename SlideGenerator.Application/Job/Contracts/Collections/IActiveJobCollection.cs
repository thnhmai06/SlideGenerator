using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Interface for managing active (pending/running/paused) jobs
/// </summary>
public interface IActiveJobCollection : IJobCollection
{
    #region Group Lifecycle

    IJobGroup CreateGroup(GenerateSlideGroupCreate request);
    void StartGroup(string groupId);
    void PauseGroup(string groupId);
    void ResumeGroup(string groupId);
    void CancelGroup(string groupId);

    #endregion

    #region Sheet Lifecycle

    void PauseSheet(string sheetId);
    void ResumeSheet(string sheetId);
    void CancelSheet(string sheetId);

    #endregion

    #region Bulk Operations

    void PauseAll();
    void ResumeAll();
    void CancelAll();

    #endregion

    #region Query

    bool HasActiveJobs { get; }
    IReadOnlyDictionary<string, IJobGroup> GetRunningGroups();
    IReadOnlyDictionary<string, IJobGroup> GetPausedGroups();
    IReadOnlyDictionary<string, IJobGroup> GetPendingGroups();

    #endregion
}