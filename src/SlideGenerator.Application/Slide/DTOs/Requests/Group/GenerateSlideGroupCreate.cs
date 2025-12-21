using System.Text.Json.Serialization;
using SlideGenerator.Application.Slide.DTOs.Components;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

/// <summary>
///     Request to create a new slide generation group.
/// </summary>
public sealed record GenerateSlideGroupCreate
{
    public string? TemplatePresentationPath { get; init; }

    [JsonPropertyName("TemplatePath")] public string? TemplatePath { get; init; }

    public string SpreadsheetPath { get; init; } = string.Empty;

    public string? FilePath { get; init; }

    [JsonPropertyName("Path")] public string? OutputPath { get; init; }

    public SlideTextConfig[]? TextConfigs { get; init; }
    public SlideImageConfig[]? ImageConfigs { get; init; }
    public string[]? SheetNames { get; init; }

    [JsonPropertyName("CustomSheet")] public string[]? CustomSheet { get; init; }

    /// <summary>
    ///     Resolves the template path from available fields.
    /// </summary>
    public string GetTemplatePath()
    {
        return !string.IsNullOrWhiteSpace(TemplatePath)
            ? TemplatePath
            : TemplatePresentationPath ?? string.Empty;
    }

    /// <summary>
    ///     Resolves the output folder path from available fields.
    /// </summary>
    public string GetOutputPath()
    {
        return !string.IsNullOrWhiteSpace(OutputPath)
            ? OutputPath
            : FilePath ?? string.Empty;
    }
}