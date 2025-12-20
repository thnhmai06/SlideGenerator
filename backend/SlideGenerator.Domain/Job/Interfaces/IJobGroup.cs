using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Composite job root.
///     A group aggregates multiple sheets and exposes group-level status/progress.
/// </summary>
public interface IJobGroup
{
    #region Identity

    /// <summary>
    ///     Gets the unique identifier of this group.
    /// </summary>
    string Id { get; }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the source workbook that the group is based on.
    /// </summary>
    ISheetBook Workbook { get; }

    /// <summary>
    ///     Gets the PowerPoint template used for sheet processing.
    /// </summary>
    ITemplatePresentation Template { get; }

    /// <summary>
    ///     Gets the output folder where generated files are stored.
    /// </summary>
    DirectoryInfo OutputFolder { get; }

    /// <summary>
    ///     Gets the current group status.
    /// </summary>
    GroupStatus Status { get; }

    /// <summary>
    ///     Gets the overall progress across all sheets (0-100).
    /// </summary>
    float Progress { get; }

    /// <summary>
    ///     Gets the UTC time when the group was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    ///     Gets the UTC time when the group finished, if any.
    /// </summary>
    DateTime? FinishedAt { get; }

    /// <summary>
    ///     Gets text replacement configs shared by all sheets.
    /// </summary>
    TextConfig[] TextConfigs { get; }

    /// <summary>
    ///     Gets image replacement configs shared by all sheets.
    /// </summary>
    ImageConfig[] ImageConfigs { get; }

    #endregion

    #region Composite - Sheet Management

    /// <summary>
    ///     Gets all sheets in this group.
    /// </summary>
    IReadOnlyDictionary<string, IJobSheet> Sheets { get; }

    /// <summary>
    ///     Gets a sheet by its identifier.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    /// <returns>The sheet if found; otherwise <c>null</c>.</returns>
    IJobSheet? GetSheet(string sheetId);

    /// <summary>
    ///     Gets the number of sheets in this group.
    /// </summary>
    int SheetCount { get; }

    /// <summary>
    ///     Checks whether the group contains a sheet.
    /// </summary>
    /// <param name="sheetId">The sheet identifier.</param>
    bool ContainsSheet(string sheetId);

    #endregion

    #region Status Query

    /// <summary>
    ///     Gets whether the group finished successfully.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    ///     Gets whether any sheet failed.
    /// </summary>
    bool HasFailed { get; }

    /// <summary>
    ///     Gets whether the group was cancelled.
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    ///     Gets whether the group is still active (not finished).
    /// </summary>
    bool IsActive { get; }

    #endregion
}