namespace SlideGenerator.Domain.Slides.Models.Identifiers;

/// <summary>
///     Identifies a specific shape within a slide.
/// </summary>
/// <param name="Slide">The identifier of the parent slide containing this shape.</param>
/// <param name="Id">The unique ID of the shape within the slide.</param>
public record ShapeIdentifier(SlideIdentifier Slide, uint Id);
