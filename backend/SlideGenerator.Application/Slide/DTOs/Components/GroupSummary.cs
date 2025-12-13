using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Components;

public record GroupSummary(
    string GroupId,
    string WorkbookPath,
    GroupStatus Status,
    float Progress,
    int TotalJobs,
    int CompletedJobs);