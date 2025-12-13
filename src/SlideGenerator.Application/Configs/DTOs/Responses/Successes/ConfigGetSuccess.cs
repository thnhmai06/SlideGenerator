using SlideGenerator.Application.Configs.DTOs.Components;
using SlideGenerator.Application.Configs.DTOs.Enums;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

public record ConfigGetSuccess(
    ServerConfig Server,
    DownloadConfig Download,
    JobConfig Job) : ConfigSuccess(ConfigRequestType.Get);