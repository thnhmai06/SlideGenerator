using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Response for job queries.
/// </summary>
public sealed record JobQuerySuccess(
    JobDetail? Job,
    IReadOnlyList<JobSummary>? Jobs)
    : Response("jobquery");