using System.Text.Json.Serialization;

namespace SlideGenerator.Domain.Features.Jobs.Enums;

/// <summary>
///     Represents the lifecycle status of a sheet job.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SheetJobStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}