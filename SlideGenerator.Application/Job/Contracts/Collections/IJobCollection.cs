using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts.Collections;

/// <summary>
///     Base interface for job collection operations (both active and completed)
/// </summary>
public interface IJobCollection
{
    #region Group Operations

    IJobGroup? GetGroup(string groupId);
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();
    int GroupCount { get; }

    #endregion

    #region Sheet Operations

    IJobSheet? GetSheet(string sheetId);
    IReadOnlyDictionary<string, IJobSheet> GetAllSheets();
    int SheetCount { get; }

    #endregion

    #region Query

    bool ContainsGroup(string groupId);
    bool ContainsSheet(string sheetId);
    bool IsEmpty { get; }

    #endregion
}