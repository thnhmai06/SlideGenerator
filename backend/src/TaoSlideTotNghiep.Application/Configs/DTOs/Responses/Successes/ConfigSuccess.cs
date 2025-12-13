using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Base.DTOs.Responses;
using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Successes;

public abstract record ConfigSuccess(ConfigRequestType Type) : SuccessResponse(RequestType.Config);