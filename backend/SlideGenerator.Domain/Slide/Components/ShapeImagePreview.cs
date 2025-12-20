namespace SlideGenerator.Domain.Slide.Components;

/// <summary>
///     Represents raw shape image data from a presentation.
/// </summary>
public record ShapeImagePreview(string Name, byte[] ImageBytes);