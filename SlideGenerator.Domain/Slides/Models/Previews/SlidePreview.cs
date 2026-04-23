namespace SlideGenerator.Domain.Slides.Models.Previews;

/// <summary>
///     Represents a preview of an entire slide.
/// </summary>
/// <param name="Index">The 1-based index of the slide within the presentation.</param>
/// <param name="Id">The unique ID of the slide.</param>
/// <param name="Name">The name of the slide, if any.</param>
/// <param name="Image">The preview image data as a byte array.</param>
public record SlidePreview(int Index, uint Id, string Name, byte[] Image) : ObjectPreview(Name, Image);
