using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Base job collection interface.
/// </summary>
public interface IJobCollection
{
    /// <summary>
    ///     Gets the count of groups.
    /// </summary>
    int GroupCount { get; }

    /// <summary>
    ///     Gets the count of sheets.
    /// </summary>
    int SheetCount { get; }

    /// <summary>
    ///     Gets a value indicating whether the collection is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    ///     Gets a group by id.
    /// </summary>
    IJobGroup? GetGroup(string groupId);

    /// <summary>
    ///     Gets all groups in the collection.
    /// </summary>
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();

    /// <summary>
    ///     Gets a sheet by id.
    /// </summary>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Gets all sheets in the collection.
    /// </summary>
    IReadOnlyDictionary<string, IJobSheet> GetAllSheets();

    /// <summary>
    ///     Checks if the group id exists in the collection.
    /// </summary>
    bool ContainsGroup(string groupId);

    /// <summary>
    ///     Checks if the sheet id exists in the collection.
    /// </summary>
    bool ContainsSheet(string sheetId);
}