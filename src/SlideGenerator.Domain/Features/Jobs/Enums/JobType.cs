using System.Text.Json.Serialization;

namespace SlideGenerator.Domain.Features.Jobs.Enums;

/// <summary>
///     Represents the job type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobType
{
    Group,
    Sheet
}