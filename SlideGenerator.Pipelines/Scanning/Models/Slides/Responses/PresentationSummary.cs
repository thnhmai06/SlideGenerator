namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Responses;

public record PresentationSummary(string PresentationPath, IReadOnlyList<SlideSummary> Slides);