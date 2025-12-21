using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Slide.DTOs.Enums;

/// <summary>
///     Control actions for job execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControlAction
{
    Pause,
    Resume,
    Cancel,
    Stop
}