using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

public abstract record ConfigSuccess(ConfigRequestType Type) : SuccessResponse(RequestType.Config);