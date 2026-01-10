using System.Text.Json.Serialization;

namespace SlideGenerator.Domain.Features.Images.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageRoiType
{
    Attention,
    Prominent,
    Center
}