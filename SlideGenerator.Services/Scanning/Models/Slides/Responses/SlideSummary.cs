using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Scanning.Models.Slides.Responses;

public sealed record SlideSummary(
    SlideIdentifier Identifier,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapeSummary> ImageShapes,
    byte[]? Preview);