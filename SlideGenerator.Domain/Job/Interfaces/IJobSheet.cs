using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Interfaces;

public interface IJobSheet
{
    #region Indetity

    string Id { get; }

    #endregion

    #region Properties

    string GroupId { get; } // parent
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
    ITemplatePresentation Template { get; } // reference to parent
    TextConfig[] TextConfigs { get; } // reference to parent
    ImageConfig[] ImageConfigs { get; } // reference to parent

    #endregion
}