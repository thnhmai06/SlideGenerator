using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Sheets.Interfaces;
using SlideGenerator.Domain.Features.Slides;

namespace SlideGenerator.Domain.Features.Jobs.Interfaces;

/// <summary>
///     Exposes a read-only view of a group job.
/// </summary>
public interface IJobGroup
{
    /// <summary>
    ///     Unique identifier for the group job.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     Workbook that provides sheet data for the group.
    /// </summary>
    ISheetBook Workbook { get; }

    /// <summary>
    ///     Template presentation used for slide generation.
    /// </summary>
    ITemplatePresentation Template { get; }

    /// <summary>
    ///     Output folder for generated presentations.
    /// </summary>
    DirectoryInfo OutputFolder { get; }

    /// <summary>
    ///     Current group lifecycle status.
    /// </summary>
    GroupStatus Status { get; }

    /// <summary>
    ///     Aggregate progress across all sheet jobs (0-100).
    /// </summary>
    float Progress { get; }

    /// <summary>
    ///     Total number of errors across sheets.
    /// </summary>
    int ErrorCount { get; }

    /// <summary>
    ///     Sheet jobs belonging to this group (id -> job).
    /// </summary>
    IReadOnlyDictionary<string, IJobSheet> Sheets { get; }

    /// <summary>
    ///     Number of sheets in this group.
    /// </summary>
    int SheetCount { get; }
}