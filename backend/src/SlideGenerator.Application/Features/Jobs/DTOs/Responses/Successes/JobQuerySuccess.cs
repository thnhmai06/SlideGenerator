using System.Text.Json.Serialization;
using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Response for job queries.
/// </summary>
public sealed record JobQuerySuccess(
    [property: JsonPropertyName("Task")] JobDetail? Job,
    [property: JsonPropertyName("Tasks")] IReadOnlyList<JobSummary>? Jobs)
    : Response("jobquery");