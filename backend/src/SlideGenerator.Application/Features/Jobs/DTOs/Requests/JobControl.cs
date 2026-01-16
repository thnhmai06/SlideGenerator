using System.Text.Json.Serialization;
using SlideGenerator.Application.Features.Slides.DTOs.Enums;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Requests;

/// <summary>
///     Request to control a job.
/// </summary>
public sealed record JobControl
{
    [JsonPropertyName("JobId")] public string JobId { get; init; } = string.Empty;

    [JsonPropertyName("JobType")] public JobType? JobType { get; init; }

    public ControlAction Action { get; init; } = ControlAction.Pause;
}