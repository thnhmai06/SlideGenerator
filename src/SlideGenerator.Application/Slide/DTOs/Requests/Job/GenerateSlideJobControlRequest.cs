using System.Text.Json.Serialization;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Job;

/// <summary>
///     Request to control a sheet job.
/// </summary>
public sealed record GenerateSlideJobControlRequest
{
    public string JobId { get; init; } = string.Empty;
    public ControlAction? Action { get; init; }

    [JsonPropertyName("State")] public ControlAction? State { get; init; }

    /// <summary>
    ///     Resolves the requested action from available fields.
    /// </summary>
    public ControlAction GetAction()
    {
        return Action ?? State ?? ControlAction.Pause;
    }
}