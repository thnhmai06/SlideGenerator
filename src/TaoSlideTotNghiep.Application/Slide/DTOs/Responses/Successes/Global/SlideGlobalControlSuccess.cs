using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Global;

public record SlideGlobalControlSuccess(ControlAction Action, int AffectedGroups, int AffectedJobs)
    : Success(SlideRequestType.GlobalControl);