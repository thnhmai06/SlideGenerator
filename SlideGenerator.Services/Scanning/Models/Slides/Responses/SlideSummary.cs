namespace SlideGenerator.Services.Scanning.Models.Slides.Responses;

public sealed record SlideSummary(
    uint SlideIndex, uint SlideId, string SlideName,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapeSummary> ImageShapes,
    byte[]? Preview);