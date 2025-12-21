using System.Text.Json.Serialization;

namespace SlideGenerator.Domain.Job.Enums;

/// <summary>
///     Represents the lifecycle status of a group job.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}