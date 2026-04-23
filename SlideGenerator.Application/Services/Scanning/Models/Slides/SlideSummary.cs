using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Application.Services.Scanning.Models.Slides;

/// <summary>
///     Describes one slide and template markers discovered during scanning.
/// </summary>
/// <param name="Index">Slide index in presentation.</param>
/// <param name="Preview">Slide preview.</param>
/// <param name="Placeholders">Detected text placeholders.</param>
/// <param name="ImageShapes">Detected image shapes with their unique identifiers.</param>
public sealed record SlideSummary(
    int Index,
    SlidePreview Preview,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapePreview> ImageShapes);