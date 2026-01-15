using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response containing model status information.
/// </summary>
public sealed record ModelStatusSuccess(
    bool FaceModelAvailable)
    : Response("modelstatus");
