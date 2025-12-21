using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Exposes a read-only view of a group job.
/// </summary>
public interface IJobGroup
{
    string Id { get; }
    ISheetBook Workbook { get; }
    ITemplatePresentation Template { get; }
    DirectoryInfo OutputFolder { get; }
    GroupStatus Status { get; }
    float Progress { get; }
    int ErrorCount { get; }
    IReadOnlyDictionary<string, IJobSheet> Sheets { get; }
    int SheetCount { get; }
}