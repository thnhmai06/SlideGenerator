namespace SlideGenerator.Scanning.Models.Slides;

/// <summary>
///     Represents slide scan response payload.
/// </summary>
/// <param name="FilePath">Scanned presentation file path.</param>
/// <param name="Slides">Collection of slide scan items.</param>
public sealed record Presentation(string FilePath, IReadOnlyList<Slide> Slides);