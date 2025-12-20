using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Main job manager interface that provides access to active and completed job collections
/// </summary>
public interface IJobManager
{
    #region Collections

    /// <summary>
    ///     Collection of active jobs (pending, running, paused)
    /// </summary>
    IActiveJobCollection Active { get; }

    /// <summary>
    ///     Collection of completed jobs (finished, failed, cancelled)
    /// </summary>
    ICompletedJobCollection Completed { get; }

    #endregion

    #region Cross-Collection Query

    /// <summary>
    ///     Get a group from either active or completed collections
    /// </summary>
    IJobGroup? GetGroup(string groupId);

    /// <summary>
    ///     Get a sheet from either active or completed collections
    /// </summary>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Get all groups from both collections
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();

    #endregion
}