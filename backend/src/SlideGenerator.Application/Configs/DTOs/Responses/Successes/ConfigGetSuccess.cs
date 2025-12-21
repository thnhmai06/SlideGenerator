using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Configs.DTOs.Components;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response containing current configuration.
/// </summary>
public sealed record ConfigGetSuccess(
    ServerConfig Server,
    DownloadConfig Download,
    JobConfig Job,
    ImageConfig Image)
    : Response("get");
