using TaoSlideTotNghiep.Domain.Sheet.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Interfaces;
using TaoSlideTotNghiep.Domain.Slide.Components;
using TaoSlideTotNghiep.Domain.Slide.Interfaces;

namespace TaoSlideTotNghiep.Domain.Job.Interfaces;

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