using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for configuration updates.
/// </summary>
public sealed record ConfigUpdateSuccess(bool Success, string Message)
    : Response("update");