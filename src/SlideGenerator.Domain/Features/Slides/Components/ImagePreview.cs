namespace SlideGenerator.Domain.Features.Slides.Components;

/// <summary>
///     Represents raw shape image data from a presentation.
/// </summary>
public record ImagePreview(string Name, byte[] Image);