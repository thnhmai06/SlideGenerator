using ImageMagick;
using OpenCvSharp;
using SlideGenerator.Application.Images.Abstractions;
using SlideGenerator.Domain.Images.Entities;
using Mat = SlideGenerator.Infrastructure.Images.Adapters.Mat;

namespace SlideGenerator.Infrastructure.Images.Services;

/// <summary>
///     ImageMagick-backed Image decoder that normalizes image input to PNG format for OpenCV processing.
/// </summary>
public sealed class MatDecoder : IImageDecoder
{
    /// <inheritdoc />
    public IImage Decode(byte[] imageBytes)
    {
        if (imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

        byte[] pngBytes;
        try
        {
            using var magickImage = new MagickImage(imageBytes);
            magickImage.Format = MagickFormat.Png;
            pngBytes = magickImage.ToByteArray();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Cannot decode image bytes using ImageMagick.", e);
        }

        var mat = Cv2.ImDecode(pngBytes, ImreadModes.Unchanged);
        if (mat.Empty())
        {
            mat.Dispose();
            throw new InvalidOperationException("Cannot decode PNG bytes from ImageMagick into OpenCV image.");
        }

        return new Mat(mat);
    }
}