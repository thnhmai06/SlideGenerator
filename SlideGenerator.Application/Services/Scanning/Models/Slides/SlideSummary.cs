using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Application.Services.Scanning.Models.Slides;

/// <summary>
///     Describes one slide and template markers discovered during scanning.
/// </summary>
/// <param name="Index">Slide index in presentation (1-based).</param>
/// <param name="Id">Unique slide ID from the presentation.</param>
/// <param name="Name">Slide name, if any.</param>
/// <param name="Preview">Slide preview image.</param>
/// <param name="Placeholders">Detected text placeholders.</param>
/// <param name="ImageShapes">Detected image shapes with their unique identifiers.</param>
public sealed record SlideSummary(
    int Index,
    uint Id,
    string Name,
    SlidePreview Preview,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapePreview> ImageShapes);
