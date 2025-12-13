using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Group;

public record GenerateSlideGroupControlRequest(string GroupId, ControlAction Action)
    : Request(SlideRequestType.GroupControl);