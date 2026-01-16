using System.Text.Json.Serialization;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Summary information for a job.
/// </summary>
public sealed record JobSummary(
    [property: JsonPropertyName("JobId")] string JobId,
    [property: JsonPropertyName("JobType")]
    JobType JobType,
    JobState Status,
    float Progress,
    string? GroupId,
    string? SheetName,
    string? OutputPath,
    int ErrorCount,
    string? HangfireJobId);