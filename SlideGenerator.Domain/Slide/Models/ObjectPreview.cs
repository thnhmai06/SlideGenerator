namespace SlideGenerator.Domain.Slide.Models;

/// <summary>
///     Represents presentation object data and its preview.
/// </summary>
/// <param name="Name">The name of the object.</param>
/// <param name="Image">The preview image data as a byte array.</param>
/// Reviewed by @thnhmai06 at 05/03/2026
public abstract record ObjectPreview(string Name, byte[] Image);