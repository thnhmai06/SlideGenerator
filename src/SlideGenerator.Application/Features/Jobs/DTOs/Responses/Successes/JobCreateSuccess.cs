using System.Text.Json.Serialization;
using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Response for job creation.
/// </summary>
public sealed record JobCreateSuccess(
    [property: JsonPropertyName("Job")] JobSummary Job,
    [property: JsonPropertyName("SheetJobIds")]
    IReadOnlyDictionary<string, string>? SheetJobIds)
    : Response("jobcreate");