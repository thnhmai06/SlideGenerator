using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Composite interface: Group contains multiple Sheets
/// </summary>
public interface IJobGroup
{
    #region Identity

    string Id { get; }

    #endregion

    #region Properties

    ISheetBook Workbook { get; }
    ITemplatePresentation Template { get; }
    DirectoryInfo OutputFolder { get; }
    GroupStatus Status { get; }
    float Progress { get; }
    DateTime CreatedAt { get; }
    DateTime? FinishedAt { get; }
    TextConfig[] TextConfigs { get; }
    ImageConfig[] ImageConfigs { get; }

    #endregion

    #region Composite - Sheet Management

    /// <summary>
    ///     All sheets in this group (composite children)
    /// </summary>
    IReadOnlyDictionary<string, IJobSheet> Sheets { get; }

    /// <summary>
    ///     Get a specific sheet by ID
    /// </summary>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Number of sheets in this group
    /// </summary>
    int SheetCount { get; }

    /// <summary>
    ///     Check if this group contains a sheet
    /// </summary>
    bool ContainsSheet(string sheetId);

    #endregion

    #region Status Query

    /// <summary>
    ///     Check if all sheets are completed (successful)
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    ///     Check if any sheet has failed
    /// </summary>
    bool HasFailed { get; }

    /// <summary>
    ///     Check if all sheets are cancelled
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    ///     Check if this group is still active (not finished)
    /// </summary>
    bool IsActive { get; }

    #endregion
}