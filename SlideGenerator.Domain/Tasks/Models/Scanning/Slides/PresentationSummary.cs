namespace SlideGenerator.Domain.Tasks.Models.Scanning.Slides;

/// <summary>
///     Presentation scan result.
/// </summary>
/// <param name="Identifier">Identified presentation.</param>
/// <param name="Slides">Scanned slide metadata.</param>
public record PresentationSummary(string FilePath, IReadOnlyList<SlideSummary> Slides)
;

