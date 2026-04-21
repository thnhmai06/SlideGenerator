using SlideGenerator.Domain.Images.Entities;

namespace SlideGenerator.Application.Images.Abstractions;

/// <summary>
///     Decodes image bytes to Image format.
/// </summary>
public interface IImageDecoder
{
    /// <summary>
    ///     Decodes encoded image bytes into an <see cref="IImage" /> instance.
    /// </summary>
    /// <param name="imageBytes">Encoded image bytes.</param>
    /// <returns>A decoded mat.</returns>
    IImage Decode(byte[] imageBytes);
}
