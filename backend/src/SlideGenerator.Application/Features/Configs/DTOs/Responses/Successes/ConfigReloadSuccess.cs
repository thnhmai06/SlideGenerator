using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for configuration reload.
/// </summary>
public sealed record ConfigReloadSuccess(bool Success, string Message)
    : Response("reload");