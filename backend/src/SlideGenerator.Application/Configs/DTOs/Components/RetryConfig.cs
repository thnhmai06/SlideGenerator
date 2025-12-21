namespace SlideGenerator.Application.Configs.DTOs.Components;

/// <summary>
///     Download retry configuration DTO.
/// </summary>
public sealed record RetryConfig(int Timeout, int MaxRetries);