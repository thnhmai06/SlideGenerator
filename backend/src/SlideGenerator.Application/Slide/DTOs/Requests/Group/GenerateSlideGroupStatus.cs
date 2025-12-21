using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

/// <summary>
///     Request to query group status.
/// </summary>
public sealed record GenerateSlideGroupStatus
{
    public string? GroupId { get; init; }
    public string? Path { get; init; }

    [JsonPropertyName("FilePath")] public string? FilePath { get; init; }

    /// <summary>
    ///     Resolves the output path from available fields.
    /// </summary>
    public string? GetOutputPath()
    {
        return !string.IsNullOrWhiteSpace(Path) ? Path : FilePath;
    }
}