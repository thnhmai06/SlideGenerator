using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Features.Slides.DTOs.Enums;

/// <summary>
///     Control actions for job execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControlAction
{
    Pause,
    Resume,
    Cancel,
    Stop,
    Remove
}