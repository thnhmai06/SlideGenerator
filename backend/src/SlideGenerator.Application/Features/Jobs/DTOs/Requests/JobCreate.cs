using System.Text.Json.Serialization;
using SlideGenerator.Application.Features.Slides.DTOs.Components;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Requests;

/// <summary>
///     Request to create a job (group or sheet).
/// </summary>
public sealed record JobCreate
{
    [JsonPropertyName("JobType")] public JobType JobType { get; init; } = JobType.Group;

    public string TemplatePath { get; init; } = string.Empty;

    public string SpreadsheetPath { get; init; } = string.Empty;

    /// <summary>
    ///     For group jobs: output folder. For sheet jobs: output file or folder.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    public string[]? SheetNames { get; init; }

    public string? SheetName { get; init; }

    public SlideTextConfig[]? TextConfigs { get; init; }

    public SlideImageConfig[]? ImageConfigs { get; init; }

    public bool AutoStart { get; init; } = true;
}