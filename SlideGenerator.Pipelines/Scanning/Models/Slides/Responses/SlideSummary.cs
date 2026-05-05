using SlideGenerator.Pipelines.Generating.Models.Identifiers;

namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Responses;

public sealed record SlideSummary(
    SlideIdentifier Identifier,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapeSummary> ImageShapes,
    byte[]? Preview);
