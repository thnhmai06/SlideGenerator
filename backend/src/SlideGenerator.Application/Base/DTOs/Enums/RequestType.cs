using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Base.DTOs.Enums;

/// <summary>
/// Types of requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestType
{
    Slide,
    Sheet,
    Config
}