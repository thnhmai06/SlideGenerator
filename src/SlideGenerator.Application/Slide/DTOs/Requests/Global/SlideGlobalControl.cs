using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Global;

public record SlideGlobalControl(ControlAction Action)
    : Request(SlideRequestType.GlobalControl);