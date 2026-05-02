using System.Drawing;
using ImageMagick;
using Syncfusion.Presentation;

namespace SlideGenerator.Slides;

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
    private const double EmuPerPixel = 9525.0;

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
    ///     Extracts a MagickImage preview of a specific shape from slide preview bytes.
    /// </summary>
    /// <param name="shape">The shape to extract preview for.</param>
    /// <param name="slidePreview">Byte array containing the full slide preview.</param>
    /// <returns>A MagickImage containing the cropped shape preview (caller responsible for disposal).</returns>
    /// <exception cref="ArgumentNullException">Thrown when shape or slidePreview is null.</exception>
    public static MagickImage GetPreviewImage(this IShape shape, byte[] slidePreview)
    {
        var bounds = shape.GetBoundsF();
        var magickImage = new MagickImage(slidePreview);
        
        magickImage.Crop(new MagickGeometry(
            (int)Math.Round(bounds.X),
            (int)Math.Round(bounds.Y),
            (uint)Math.Round(bounds.Width),
            (uint)Math.Round(bounds.Height)));

        return magickImage;
    }

    /// <summary>
    ///     Extracts a MagickImage preview of a specific shape from an existing MagickImage.
    /// </summary>
    /// <param name="shape">The shape to extract preview for.</param>
    /// <param name="slidePreviewImage">The MagickImage containing the full slide preview.</param>
    /// <returns>A cloned MagickImage containing the cropped shape preview (caller responsible for disposal).</returns>
    /// <remarks>
    ///     This method clones the input image to avoid modifying the original.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when shape or slidePreviewImage is null.</exception>
    public static MagickImage GetPreviewImage(this IShape shape, MagickImage slidePreviewImage)
    {
        var bounds = shape.GetBoundsF();
        var croppedImage = (MagickImage)slidePreviewImage.Clone();
        
        croppedImage.Crop(new MagickGeometry(
            (int)Math.Round(bounds.X),
            (int)Math.Round(bounds.Y),
            (uint)Math.Round(bounds.Width),
            (uint)Math.Round(bounds.Height)));

        return croppedImage;
    }

    /// <summary>
    ///     Extracts a PNG byte array preview of a specific shape from slide preview bytes.
    /// </summary>
    /// <remarks>
    ///     This method maintains backward compatibility by returning byte array format.
    ///     Internally uses GetPreviewImage for processing.
    /// </remarks>
    /// <param name="shape">The shape to extract preview for.</param>
    /// <param name="slidePreview">Byte array containing the full slide preview.</param>
    /// <returns>Byte array containing PNG image data of the shape.</returns>
    /// <exception cref="ArgumentNullException">Thrown when shape or slidePreview is null.</exception>
    public static byte[] GetPreview(this IShape shape, byte[] slidePreview)
    {
        using var previewImage = shape.GetPreviewImage(slidePreview);
        return previewImage.ToByteArray(MagickFormat.Png);
    }

    /// <summary>
    ///     Gets the bounding rectangle of a shape in pixels.
    /// </summary>
    /// <param name="shape">The shape to get bounds for.</param>
    /// <returns>RectangleF containing the position and size in pixel coordinates.</returns>
    /// <remarks>
    ///     Synfusion shapes use EMU (English Metric Units) internally. This method converts
    ///     to pixel coordinates using the standard conversion factor (1 pixel = 9525 EMU).
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when shape is null.</exception>
    public static RectangleF GetBoundsF(this IShape shape)
    {
        return new RectangleF(
            (float)(shape.Left / EmuPerPixel),
            (float)(shape.Top / EmuPerPixel),
            (float)(shape.Width / EmuPerPixel),
            (float)(shape.Height / EmuPerPixel));
    }
}