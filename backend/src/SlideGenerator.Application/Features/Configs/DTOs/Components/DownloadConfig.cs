namespace SlideGenerator.Application.Features.Configs.DTOs.Components;

/// <summary>
///     Download configuration DTO.
/// </summary>
public sealed record DownloadConfig(
    int MaxChunks,
    int LimitBytesPerSecond,
    string SaveFolder,
    RetryConfig Retry);