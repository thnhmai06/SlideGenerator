using System.Drawing;

namespace SlideGenerator.Domain.Images.Entities;

/// <summary>
///     Represents an image abstraction.
/// </summary>
public interface IImage : IDisposable, ICloneable
{
    /// <summary>
    ///     Gets the width of the image in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    ///     Gets the height of the image in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    ///     Gets the size of the image.
    /// </summary>
    Size Size => new(Width, Height);

    /// <summary>
    ///     Checks if the image is empty.
    /// </summary>
    /// <returns><see langword="true" /> if empty; otherwise, <see langword="false" />.</returns>
    bool Empty();

    /// <summary>
    ///     Gets the number of channels in the image.
    /// </summary>
    /// <returns>The channel count.</returns>
    int Channels();

    /// <summary>
    ///     Converts the image to a byte array.
    /// </summary>
    /// <returns>A <see langword="byte" /> array representing the image data.</returns>
    byte[] ToByteArray();
}