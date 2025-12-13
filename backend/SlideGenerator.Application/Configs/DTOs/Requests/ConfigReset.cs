using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Requests;

public record ConfigReset() : ConfigRequest(ConfigRequestType.Reset);