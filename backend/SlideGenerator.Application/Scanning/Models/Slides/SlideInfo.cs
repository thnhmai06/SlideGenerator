namespace SlideGenerator.Application.Scanning.Models.Slides;

/// <summary>
///     Represents scan result for a single slide.
/// </summary>
/// <param name="Index">1-based slide index.</param>
/// <param name="ImageShapeIds">Detected image-capable shape ids.</param>
/// <param name="Mustaches">Detected text mustaches.</param>
public sealed record SlideInfo(
    int Index,
    IReadOnlyList<string> Mustaches,
    IReadOnlyList<uint> ImageShapeIds);