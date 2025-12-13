using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;

public record SlideGroupCreateSuccess(string GroupId, string OutputFolder, Dictionary<string, string> JobIds)
    : Success(SlideRequestType.GroupCreate);