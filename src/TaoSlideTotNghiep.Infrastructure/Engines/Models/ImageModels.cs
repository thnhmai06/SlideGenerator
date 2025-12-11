using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using ImageMagick;
using System.Runtime.InteropServices;
using TaoSlideTotNghiep.Infrastructure.Exceptions;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Models;

/// <summary>
/// Represents an image.
/// Provides methods for saliency detection and cropping.
/// </summary>
public class Image : EngineModel,
    IDisposable
{
    private bool _disposed;

    public string FilePath { get; }

    public Mat Mat { get; set; }

    public Image(string filePath)
    {
        FilePath = filePath;
        if (!File.Exists(FilePath))
            throw new FileNotFoundException("Image file not found.", filePath);

        using var magickImage = new MagickImage(filePath);
        var pixels = magickImage.GetPixels();
        var bytes = pixels.ToByteArray(PixelMapping.BGR);
        if (bytes == null)
            throw new ReadImageFailedException(filePath);

        Mat = new Mat((int)magickImage.Height, (int)magickImage.Width, DepthType.Cv8U, 3);
        Marshal.Copy(bytes, 0, Mat.DataPointer, bytes.Length);
        if (Mat.IsEmpty)
            throw new ReadImageFailedException(filePath);
    }

    /// <summary>
    /// Saves the current image to the specified file path.
    /// </summary>
    public void Save(string? savePath = null)
    {
        savePath ??= FilePath;

        using var buffer = new VectorOfByte();
        CvInvoke.Imencode(".png", Mat, buffer);
        var bytes = buffer.ToArray();

        using var magick = new MagickImage(bytes);
        magick.Write(savePath);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Mat.Dispose();
        GC.SuppressFinalize(this);
    }
}