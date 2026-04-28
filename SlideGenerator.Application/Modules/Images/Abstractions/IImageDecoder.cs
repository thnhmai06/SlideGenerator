using SlideGenerator.Domain.Images.Entities;

namespace SlideGenerator.Application.Modules.Images.Abstractions;

/// <summary>
///     Defines a contract for decoding raw image bytes into an <see cref="IImage" /> representation.
/// </summary>
public interface IImageDecoder
{
    /// <summary>
    ///     Decodes encoded image bytes into an <see cref="IImage" /> instance.
    /// </summary>
    /// <param name="imageBytes">The raw encoded image bytes.</param>
    /// <returns>A decoded image object implementing <see cref="IImage" />.</returns>
    IImage Decode(byte[] imageBytes);
}