using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Interface for managing completed (finished/failed/cancelled) jobs
/// </summary>
public interface ICompletedJobCollection : IJobCollection
{
    #region Remove Operations

    bool RemoveGroup(string groupId);
    bool RemoveSheet(string sheetId);
    void ClearAll();

    #endregion

    #region Query by Status

    IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups();
    IReadOnlyDictionary<string, IJobGroup> GetFailedGroups();
    IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups();

    #endregion
}