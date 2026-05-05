using SlideGenerator.Documents.Slides.Models;

namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Responses;

public sealed record SlideSummary(
    SlideIdentifier Identifier,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapeSummary> ImageShapes,
    byte[]? Preview);