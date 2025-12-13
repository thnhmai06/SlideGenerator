using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Group;

public record SlideGroupControlSuccess(string GroupId, ControlAction Action)
    : Success(SlideRequestType.GroupControl);