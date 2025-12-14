namespace SlideGenerator.Domain.Slide.Components;

/// <summary>
///     Represents raw shape image data from a presentation.
/// </summary>
public record ShapeImageData(string Name, byte[] ImageBytes);