namespace SlideGenerator.Domain.Slide.Models.Identifiers;

/// <summary>
///     Represents a shape source.
/// </summary>
/// <param name="Slide">The slide contains this shape.</param>
/// <param name="Id">The ID of shape in Slide.</param>
public record ShapeIdentifier(SlideIdentifier Slide, uint Id);