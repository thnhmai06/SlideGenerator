namespace SlideGenerator.Application.Features.Configs.DTOs.Requests;

/// <summary>
///     Request to control a model (init/deinit).
/// </summary>
public sealed record ModelControl(
    string Model,
    string Action);
