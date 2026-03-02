namespace SlideGenerator.Services.Generating.Models.Info;

/// <summary>
///     Represents a shape source.
/// </summary>
/// <param name="Slide">The slide contains this shape.</param>
/// <param name="Id">The ID of shape in Slide.</param>
public record ShapeInfo(SlideInfo Slide, uint Id);