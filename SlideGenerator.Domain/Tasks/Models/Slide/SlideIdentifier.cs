namespace SlideGenerator.Domain.Tasks.Models.Slide;

/// <summary>
///     Represents a slide source.
/// </summary>
/// <param name="Id">1-based slide index in template presentation.</param>
public sealed record SlideIdentifier(PresentationIdentifier Presentation, uint Id);