using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Requests;

/// <summary>
///     Defines job query scope.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobQueryScope
{
    Active,
    Completed,
    All
}