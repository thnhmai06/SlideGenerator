using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for configuration reset.
/// </summary>
public sealed record ConfigResetSuccess(bool Success, string Message)
    : Response("reset");