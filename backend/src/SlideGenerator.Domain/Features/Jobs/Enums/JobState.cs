using System.Text.Json.Serialization;

namespace SlideGenerator.Domain.Features.Jobs.Enums;

/// <summary>
///     Represents the lifecycle status of a job (group or sheet).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobState
{
    Pending,
    Processing,
    Paused,
    Done,
    Cancelled,
    Error
}