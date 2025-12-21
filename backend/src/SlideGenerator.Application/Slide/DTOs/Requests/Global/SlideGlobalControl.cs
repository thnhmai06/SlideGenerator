using System.Text.Json.Serialization;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Global;

/// <summary>
///     Request to control all running jobs.
/// </summary>
public sealed record SlideGlobalControl
{
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