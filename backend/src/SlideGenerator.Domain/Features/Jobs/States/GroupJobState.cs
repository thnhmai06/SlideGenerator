using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Domain.Features.Jobs.States;

/// <summary>
///     Persisted state for a group job.
/// </summary>
public sealed record GroupJobState(
    string Id,
    string WorkbookPath,
    string TemplatePath,
    string OutputFolderPath,
    GroupStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> SheetIds,
    int ErrorCount);