using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Provides access to active and completed job collections.
/// </summary>
public interface IJobManager
{
    /// <summary>
    ///     Active (pending/running/paused) job collection.
    /// </summary>
    IActiveJobCollection Active { get; }

    /// <summary>
    ///     Completed/failed/cancelled job collection.
    /// </summary>
    ICompletedJobCollection Completed { get; }

    /// <summary>
    ///     Gets a job group by id from either collection.
    /// </summary>
    IJobGroup? GetGroup(string groupId);

    /// <summary>
    ///     Gets a sheet job by id from either collection.
    /// </summary>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Gets all job groups across active and completed collections.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();
}