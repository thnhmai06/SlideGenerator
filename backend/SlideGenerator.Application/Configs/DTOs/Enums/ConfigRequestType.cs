using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Configs.DTOs.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConfigRequestType
{
    Get,
    Update,
    Reload,
    Reset
}