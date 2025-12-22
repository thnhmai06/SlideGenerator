namespace SlideGenerator.Application.Configs.DTOs.Components;

/// <summary>
///     Image configuration DTO.
/// </summary>
public sealed record ImageConfig(
    FaceConfig Face,
    SaliencyConfig Saliency);

/// <summary>
///     Face detection configuration DTO.
/// </summary>
public sealed record FaceConfig(
    float Confidence,
    float PaddingTop,
    float PaddingBottom,
    float PaddingLeft,
    float PaddingRight,
    bool UnionAll);

/// <summary>
///     Saliency configuration DTO.
/// </summary>
public sealed record SaliencyConfig(
    float PaddingTop,
    float PaddingBottom,
    float PaddingLeft,
    float PaddingRight);