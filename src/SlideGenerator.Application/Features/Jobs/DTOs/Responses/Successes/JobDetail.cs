using System.Text.Json.Serialization;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Detailed job information.
/// </summary>
public sealed record JobDetail(
    [property: JsonPropertyName("JobId")] string JobId,
    [property: JsonPropertyName("JobType")]
    JobType JobType,
    JobState Status,
    float Progress,
    int ErrorCount,
    string? ErrorMessage,
    string? GroupId,
    string? SheetName,
    int? CurrentRow,
    int? TotalRows,
    string? OutputPath,
    string? OutputFolder,
    IReadOnlyDictionary<string, JobSummary>? Sheets,
    string? PayloadJson,
    string? HangfireJobId);