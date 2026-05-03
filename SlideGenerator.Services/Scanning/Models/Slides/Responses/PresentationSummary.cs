namespace SlideGenerator.Services.Scanning.Models.Slides.Responses;

public record PresentationSummary(string PresentationPath, IReadOnlyList<SlideSummary> Slides);