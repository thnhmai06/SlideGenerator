using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;

/// <summary>
///     Response for model initialization/deinitialization operations.
/// </summary>
public sealed record ModelControlSuccess(
    string Model,
    string Action,
    bool Success,
    string? Message = null)
    : Response("modelcontrol");
