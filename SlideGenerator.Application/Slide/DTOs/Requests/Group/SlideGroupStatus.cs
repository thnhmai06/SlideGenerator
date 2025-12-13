using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

public record GenerateSlideGroupStatus(string GroupId)
    : Request(SlideRequestType.GroupStatus);