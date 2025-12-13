using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Group;

public record SlideGroupCreateSuccess(string GroupId, string OutputFolder, Dictionary<string, string> JobIds)
    : Success(SlideRequestType.GroupCreate);