using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Global;

public record SlideGlobalGetGroupsSuccess(List<GroupSummary> Groups)
    : Success(SlideRequestType.GetAllGroups);