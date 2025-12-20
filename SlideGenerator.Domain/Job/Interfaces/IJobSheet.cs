using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Composite leaf.
///     Represents a single sheet job within a group.
/// </summary>
public interface IJobSheet
{
    #region Identity

    /// <summary>
    ///     Gets the unique identifier of this sheet job.
    /// </summary>
    string Id { get; }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the parent group identifier.
    /// </summary>
    string GroupId { get; }

    /// <summary>
    ///     Gets the sheet name in the source workbook.
    /// </summary>
    string SheetName { get; }

    /// <summary>
    ///     Gets the output file path for this sheet.
    /// </summary>
    string OutputPath { get; }

    /// <summary>
    ///     Gets the current sheet execution status.
    /// </summary>
    SheetJobStatus Status { get; }

    /// <summary>
    ///     Gets the current processed row (1-based).
    /// </summary>
    int CurrentRow { get; }

    /// <summary>
    ///     Gets the total number of rows to process.
    /// </summary>
    int TotalRows { get; }

    /// <summary>
    ///     Gets progress in percent (0-100).
    /// </summary>
    float Progress { get; }

    /// <summary>
    ///     Gets an error message if the sheet failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the UTC time when the sheet job was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    ///     Gets the UTC time when processing started, if any.
    /// </summary>
    DateTime? StartedAt { get; }

    /// <summary>
    ///     Gets the UTC time when processing completed, if any.
    /// </summary>
    DateTime? CompletedAt { get; }

    /// <summary>
    ///     Gets the underlying worksheet for reading row data.
    /// </summary>
    ISheet Worksheet { get; }

    /// <summary>
    ///     Gets the group's template presentation.
    /// </summary>
    ITemplatePresentation Template { get; }

    /// <summary>
    ///     Gets the group's text configs.
    /// </summary>
    TextConfig[] TextConfigs { get; }

    /// <summary>
    ///     Gets the group's image configs.
    /// </summary>
    ImageConfig[] ImageConfigs { get; }

    #endregion
}