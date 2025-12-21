using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;

/// <summary>
///     Summary information for a group job.
/// </summary>
public sealed record GroupSummary(
    string GroupId,
    string WorkbookPath,
    GroupStatus Status,
    float Progress,
    int SheetCount,
    int CompletedSheets,
    int ErrorCount = 0);