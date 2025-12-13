using System.Text.Json.Serialization;

namespace TaoSlideTotNghiep.Domain.Image.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageRoiType
{
    Prominent,
    Center
}