using OpenCvSharp;
using SlideGenerator.Domain.Images.Entities;

namespace SlideGenerator.Infrastructure.Images.Adapters;

/// <summary>
///     Adapts an <see cref="OpenCvSharp.Mat" /> to the <see cref="IImage" /> domain entity.
/// </summary>
/// <param name="mat">The underlying OpenCV Mat instance, or <see langword="null" /> to create an empty one.</param>
public class Mat(OpenCvSharp.Mat? mat) : IImage
{
    /// <summary>
    ///     The underlying <see cref="OpenCvSharp.Mat" /> instance.
    /// </summary>
    internal readonly OpenCvSharp.Mat Core = mat ?? new OpenCvSharp.Mat();

    /// <inheritdoc />
    /// <summary>
    ///     Gets the width of the image.
    /// </summary>
    public int Width => Core.Width;

    /// <inheritdoc />
    /// <summary>
    ///     Gets the height of the image.
    /// </summary>
    public int Height => Core.Height;

    /// <summary>
    ///     Checks if the image is empty.
    /// </summary>
    /// <returns><see langword="true" /> if the image is empty; otherwise, <see langword="false" />.</returns>
    public bool Empty()
    {
        return Core.Empty();
    }

    /// <summary>
    ///     Gets the number of channels in the image.
    /// </summary>
    /// <returns>The number of channels (e.g., 3 for RGB).</returns>
    public int Channels()
    {
        return Core.Channels();
    }

    /// <inheritdoc />
    /// <summary>
    ///     Creates a shallow copy of the image.
    /// </summary>
    /// <returns>A new <see cref="Mat" /> instance that is a copy of this one.</returns>
    public object Clone()
    {
        return new Mat(Core.Clone());
    }

    /// <inheritdoc />
    /// <summary>
    ///     Encodes the image into a PNG byte array.
    /// </summary>
    /// <returns>A byte array containing the PNG-encoded image.</returns>
    /// <exception cref="InvalidOperationException">Thrown if encoding fails.</exception>
    public byte[] ToByteArray()
    {
        return Cv2.ImEncode(".png", Core, out var bytes)
            ? bytes
            : throw new InvalidOperationException("Cannot encode mat to PNG bytes.");
    }

    /// <inheritdoc />
    /// <summary>
    ///     Disposes the underlying OpenCV Mat resource.
    /// </summary>
    public void Dispose()
    {
        Core.Dispose();
        GC.SuppressFinalize(this);
    }
}