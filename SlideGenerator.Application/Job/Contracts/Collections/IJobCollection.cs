using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Base contract for a job collection.
///     Provides read-only access to groups and sheets.
/// </summary>
public interface IJobCollection
{
    #region Group Operations

    /// <summary>
    ///     Gets a group by its identifier.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The group if found; otherwise <c>null</c>.</returns>
    IJobGroup? GetGroup(string groupId);

    /// <summary>
    ///     Gets all groups in this collection.
    /// </summary>
    /// <returns>A read-only dictionary of group id to group.</returns>
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();

    /// <summary>
    ///     Gets the number of groups in this collection.
    /// </summary>
    int GroupCount { get; }

    #endregion

    #region Sheet Operations

    /// <summary>
    ///     Gets a sheet by its identifier.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    /// <returns>The sheet if found; otherwise <c>null</c>.</returns>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Gets all sheets in this collection.
    /// </summary>
    /// <returns>A read-only dictionary of sheet id to sheet.</returns>
    IReadOnlyDictionary<string, IJobSheet> GetAllSheets();

    /// <summary>
    ///     Gets the number of sheets in this collection.
    /// </summary>
    int SheetCount { get; }

    #endregion

    #region Query

    /// <summary>
    ///     Checks whether a group exists in this collection.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    bool ContainsGroup(string groupId);

    /// <summary>
    ///     Checks whether a sheet exists in this collection.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    bool ContainsSheet(string sheetId);

    /// <summary>
    ///     Gets whether this collection is empty.
    /// </summary>
    bool IsEmpty { get; }

    #endregion
}