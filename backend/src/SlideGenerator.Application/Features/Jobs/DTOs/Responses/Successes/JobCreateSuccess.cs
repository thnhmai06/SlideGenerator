using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Response for job creation.
/// </summary>
public sealed record JobCreateSuccess(
    JobSummary Job,
    IReadOnlyDictionary<string, string>? SheetJobIds)
    : Response("jobcreate");