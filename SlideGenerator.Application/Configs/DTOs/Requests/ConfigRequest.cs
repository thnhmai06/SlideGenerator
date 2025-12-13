using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Requests;

public abstract record ConfigRequest(ConfigRequestType Type) : Base.DTOs.Requests.Request(RequestType.Config);