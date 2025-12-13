using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

public record ConfigUpdateSuccess(bool Updated, string Message) : ConfigSuccess(ConfigRequestType.Update);