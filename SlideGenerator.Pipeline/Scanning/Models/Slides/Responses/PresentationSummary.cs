namespace SlideGenerator.Pipeline.Scanning.Models.Slides.Responses;

public record PresentationSummary(string PresentationPath, IReadOnlyList<SlideSummary> Slides);