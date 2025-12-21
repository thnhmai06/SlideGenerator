using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for configuration reload.
/// </summary>
public sealed record ConfigReloadSuccess(bool Success, string Message)
    : Response("reload");