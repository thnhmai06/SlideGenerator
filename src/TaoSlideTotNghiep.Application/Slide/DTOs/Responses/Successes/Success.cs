using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Base.DTOs.Responses;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes;

public abstract record Success(SlideRequestType Type)
    : SuccessResponse(RequestType.Slide);