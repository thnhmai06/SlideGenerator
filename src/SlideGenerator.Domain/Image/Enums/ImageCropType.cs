using System.Text.Json.Serialization;

namespace SlideGenerator.Domain.Image.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageCropType
{
    Crop,
    Fit
}