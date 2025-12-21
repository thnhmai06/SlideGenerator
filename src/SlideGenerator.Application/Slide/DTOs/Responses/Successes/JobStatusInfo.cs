using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes;

/// <summary>
///     Status information for a sheet job.
/// </summary>
public sealed record JobStatusInfo(
    string JobId,
    string SheetName,
    SheetJobStatus Status,
    int CurrentRow,
    int TotalRows,
    float Progress,
    string? ErrorMessage,
    int ErrorCount = 0);