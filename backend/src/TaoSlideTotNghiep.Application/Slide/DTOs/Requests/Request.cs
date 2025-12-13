using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests;

public abstract record Request(SlideRequestType Type) : Base.DTOs.Requests.Request(RequestType.Slide);