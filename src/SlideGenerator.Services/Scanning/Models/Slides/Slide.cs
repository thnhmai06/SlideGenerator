namespace SlideGenerator.Services.Scanning.Models.Slides;

/// <summary>
///     Represents scan result for a single slide.
/// </summary>
/// <param name="Index">1-based slide index.</param>
/// <param name="ImageShapeIds">Detected image-capable shape ids.</param>
/// <param name="Placeholders">Detected text placeholders.</param>
public sealed record Slide(
    int Index,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<uint> ImageShapeIds);