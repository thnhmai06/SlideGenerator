using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Global;

public record SlideGlobalControl(ControlAction Action)
    : Request(SlideRequestType.GlobalControl);