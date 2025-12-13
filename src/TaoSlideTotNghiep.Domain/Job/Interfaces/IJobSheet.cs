using TaoSlideTotNghiep.Domain.Sheet.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Interfaces;
using TaoSlideTotNghiep.Domain.Slide.Components;
using TaoSlideTotNghiep.Domain.Slide.Interfaces;

namespace TaoSlideTotNghiep.Domain.Job.Interfaces;

public interface IJobSheet
{
    string Id { get; }
    string GroupId { get; }
    string SheetName { get; }
    string OutputPath { get; }
    SheetJobStatus Status { get; }
    int CurrentRow { get; }
    int TotalRows { get; }
    float Progress { get; }
    string? ErrorMessage { get; }
    DateTime CreatedAt { get; }
    DateTime? StartedAt { get; }
    DateTime? CompletedAt { get; }

    ISheet Worksheet { get; }
    ITemplatePresentation Template { get; }
    TextConfig[] TextConfigs { get; }
    ImageConfig[] ImageConfigs { get; }
}