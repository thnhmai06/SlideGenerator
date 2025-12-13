using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes;

public abstract record Success(SlideRequestType Type)
    : SuccessResponse(RequestType.Slide);