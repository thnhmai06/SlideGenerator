using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Group;

public record GenerateSlideGroupStatus(string GroupId)
    : Request(SlideRequestType.GroupStatus);