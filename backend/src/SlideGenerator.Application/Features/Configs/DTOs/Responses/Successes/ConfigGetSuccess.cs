using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Configs.DTOs.Components;

namespace SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response containing current configuration.
/// </summary>
public sealed record ConfigGetSuccess(
    ServerConfig Server,
    DownloadConfig Download,
    JobConfig Job,
    ImageConfig Image)
    : Response("get");