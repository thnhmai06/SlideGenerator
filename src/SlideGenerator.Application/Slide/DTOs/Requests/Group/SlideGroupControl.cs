using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

public record GenerateSlideGroupControlRequest(string GroupId, ControlAction Action)
    : Request(SlideRequestType.GroupControl);