namespace SlideGenerator.Application.Features.Configs.DTOs.Components;

/// <summary>
///     Server configuration DTO.
/// </summary>
public sealed record ServerConfig(string Host, int Port, bool Debug);