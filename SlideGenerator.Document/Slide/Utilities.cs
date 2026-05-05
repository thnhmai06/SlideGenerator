using System.Drawing;
using Syncfusion.Presentation;

namespace SlideGenerator.Document.Slide;

/// <summary>
///     Provides utility methods for Syncfusion presentation shape operations and image handling.
/// </summary>
/// <remarks>
///     This utility class provides extension methods for working with Syncfusion shapes,
///     particularly for extracting and manipulating shape previews using ImageMagick format.
/// </remarks>
public static class Utilities
{
    /// <summary>
    ///     Conversion factor from EMU (English Metric Units) to pixels.
    ///     1 pixel = 9525 EMU (Syncfusion standard).
    /// </summary>
    private const float EmuPerPixel = 9525.0f;

    /// <summary>
    ///     Converts a slide to a PNG preview byte array.
    /// </summary>
    /// <param name="slide">The slide to preview.</param>
    /// <returns>Byte array containing PNG image data of the entire slide.</returns>
    /// <exception cref="ArgumentNullException">Thrown when slide is null.</exception>
    public static byte[] GetPreview(this ISlide slide)
    {
        using var stream = slide.ConvertToImage(ExportImageFormat.Png);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    ///     Gets the bounding rectangle of a shape in pixels.
    /// </summary>
    /// <param name="shape">The shape to get bounds for.</param>
    /// <returns>RectangleF containing the position and size in pixel coordinates.</returns>
    /// <remarks>
    ///     Syncfusion shapes use EMU (English Metric Units) internally. This method converts
    ///     to pixel coordinates using the standard conversion factor (1 pixel = 9525 EMU).
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the shape is null.</exception>
    public static RectangleF GetBoundsF(this IShape shape)
    {
        return new RectangleF(
            (float)(shape.Left / EmuPerPixel),
            (float)(shape.Top / EmuPerPixel),
            (float)(shape.Width / EmuPerPixel),
            (float)(shape.Height / EmuPerPixel));
    }
}