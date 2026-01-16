using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Requests;

/// <summary>
///     Request to query jobs.
/// </summary>
public sealed record JobQuery
{
    public string? JobId { get; init; }

    public JobType? JobType { get; init; }

    public JobQueryScope Scope { get; init; } = JobQueryScope.All;

    public bool IncludeSheets { get; init; } = true;

    public bool IncludePayload { get; init; }
}