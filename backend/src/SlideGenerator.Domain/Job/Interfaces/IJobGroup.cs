using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Interfaces;

public interface IJobGroup
{
    string Id { get; }
    ISheetBook Workbook { get; }
    ITemplatePresentation Template { get; }
    string OutputFolder { get; }
    GroupStatus Status { get; }
    float Progress { get; }
    IReadOnlyDictionary<string, IJobSheet> Jobs { get; }
    DateTime CreatedAt { get; }
    DateTime? FinishedAt { get; }

    TextConfig[] TextConfigs { get; }
    ImageConfig[] ImageConfigs { get; }
}