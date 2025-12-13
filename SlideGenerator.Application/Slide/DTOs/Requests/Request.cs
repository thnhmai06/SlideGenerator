using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests;

public abstract record Request(SlideRequestType Type) : Base.DTOs.Requests.Request(RequestType.Slide);