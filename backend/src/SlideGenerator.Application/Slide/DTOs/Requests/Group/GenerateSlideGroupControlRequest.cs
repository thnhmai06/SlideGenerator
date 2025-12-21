using System.Text.Json.Serialization;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

/// <summary>
///     Request to control a group job.
/// </summary>
public sealed record GenerateSlideGroupControlRequest
{
    public ControlAction? Action { get; init; }

    [JsonPropertyName("State")] public ControlAction? State { get; init; }

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

    /// <summary>
    ///     Resolves the requested action from available fields.
    /// </summary>
    public ControlAction GetAction()
    {
        return Action ?? State ?? ControlAction.Pause;
    }
}