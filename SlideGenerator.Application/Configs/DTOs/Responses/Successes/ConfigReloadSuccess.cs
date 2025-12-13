using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

public record ConfigReloadSuccess(bool Reloaded, string Message) : ConfigSuccess(ConfigRequestType.Reload);