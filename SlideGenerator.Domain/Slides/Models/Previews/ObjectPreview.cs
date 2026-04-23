namespace SlideGenerator.Domain.Slides.Models.Previews;

/// <summary>
///     Represents presentation object data and its preview image.
/// </summary>
/// <param name="Name">The name of the object.</param>
/// <param name="Image">The preview image data as a byte array.</param>
/// <remarks>Reviewed by @thnhmai06 at 05/03/2026</remarks>
public abstract record ObjectPreview(string Name, byte[] Image);
