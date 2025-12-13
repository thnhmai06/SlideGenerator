using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

public record ConfigResetSuccess(bool Reset, string Message) : ConfigSuccess(ConfigRequestType.Reset);