using System.Text.Json.Serialization;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Summary information for a job.
/// </summary>
public sealed record JobSummary(
    [property: JsonPropertyName("TaskId")] string JobId,
    [property: JsonPropertyName("TaskType")]
    JobType JobType,
    JobState Status,
    float Progress,
    string? GroupId,
    string? SheetName,
    string? OutputPath,
    int ErrorCount,
    string? HangfireJobId);