using SlideGenerator.Domain.Features.Jobs.Components;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Domain.Features.Jobs.States;

/// <summary>
///     Persisted state for a sheet job.
/// </summary>
public sealed record SheetJobState(
    string Id,
    string GroupId,
    string SheetName,
    string OutputPath,
    SheetJobStatus Status,
    int NextRowIndex,
    int TotalRows,
    int ErrorCount,
    string? ErrorMessage,
    JobTextConfig[] TextConfigs,
    JobImageConfig[] ImageConfigs);