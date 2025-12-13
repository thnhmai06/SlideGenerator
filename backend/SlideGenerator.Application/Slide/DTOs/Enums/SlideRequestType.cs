using System.Text.Json.Serialization;

namespace SlideGenerator.Application.Slide.DTOs.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SlideRequestType
{
    ScanShapes,
    GroupCreate,
    GroupStatus,
    GroupControl,
    JobControl,
    JobStatus,
    GlobalControl,
    GetAllGroups
}