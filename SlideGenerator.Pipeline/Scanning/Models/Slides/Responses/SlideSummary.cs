using SlideGenerator.Document.Slide.Models;

namespace SlideGenerator.Pipeline.Scanning.Models.Slides.Responses;

public sealed record SlideSummary(
    SlideIdentifier Identifier,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapeSummary> ImageShapes,
    byte[]? Preview);