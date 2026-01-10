using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for configuration reset.
/// </summary>
public sealed record ConfigResetSuccess(bool Success, string Message)
    : Response("reset");