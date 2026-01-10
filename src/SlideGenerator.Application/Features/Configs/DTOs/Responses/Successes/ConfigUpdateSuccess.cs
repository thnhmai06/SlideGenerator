using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for configuration updates.
/// </summary>
public sealed record ConfigUpdateSuccess(bool Success, string Message)
    : Response("update");