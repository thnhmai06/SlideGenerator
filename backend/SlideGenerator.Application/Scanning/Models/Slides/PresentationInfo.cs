namespace SlideGenerator.Application.Scanning.Models.Slides;

/// <summary>
///     Represents slide scan response payload.
/// </summary>
/// <param name="FilePath">Scanned presentation file path.</param>
/// <param name="Slides">Collection of slide scan items.</param>
public sealed record PresentationInfo(string FilePath, IReadOnlyList<SlideInfo> Slides);