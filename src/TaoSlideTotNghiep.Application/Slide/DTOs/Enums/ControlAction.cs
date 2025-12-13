using System.Text.Json.Serialization;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControlAction
{
    Pause,
    Resume,
    Cancel
}