namespace SlideGenerator.Application.Services.Scanning.Models.Slides;

/// <summary>
///     Presentation scan result.
/// </summary>
/// <param name="FilePath">Identified presentation file path.</param>
/// <param name="Slides">Scanned slide metadata.</param>
public record PresentationSummary(string FilePath, IReadOnlyList<SlideSummary> Slides);