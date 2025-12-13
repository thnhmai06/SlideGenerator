using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Interfaces;

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