using System.Drawing;
using System.Runtime.InteropServices;
using Domain.Exceptions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Saliency;
using ImageMagick;

namespace Domain.Models;

/// <summary>
/// Represents an image.
/// Provides methods for saliency detection and cropping.
/// </summary>
public class Image : Model,
    IDisposable
{
    private Mat _mat;
    private bool _disposed;

    public string FilePath { get; }

    public int Width => _mat.Width;
    public int Height => _mat.Height;

    /// <summary>
    /// Initializes the Image by loading the image from the specified file path.
    /// </summary>
    public Image(string filePath)
    {
        FilePath = filePath;

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found.", filePath);

        _mat = LoadImage(filePath);

        if (_mat.IsEmpty)
            throw new ReadImageFailedException(filePath);
    }

    /// <summary>
    /// Loads image using ImageMagick and converts to OpenCV Mat.
    /// </summary>
    private static Mat LoadImage(string filePath)
    {
        using var magickImage = new MagickImage(filePath);

        // Convert to BGR format for OpenCV
        var pixels = magickImage.GetPixels();
        var bytes = pixels.ToByteArray(PixelMapping.BGR);

        if (bytes == null)
            throw new ReadImageFailedException(filePath);

        var mat = new Mat((int)magickImage.Height, (int)magickImage.Width, DepthType.Cv8U, 3);
        Marshal.Copy(bytes, 0, mat.DataPointer, bytes.Length);

        return mat;
    }

    /// <summary>
    /// Computes the saliency map of the image using Spectral Residual method.
    /// </summary>
    /// <returns>Saliency map as a Mat (float values 0-1).</returns>
    public Mat ComputeSaliency()
    {
        using var saliency = new StaticSaliencySpectralResidual();
        using var saliencyMap = new Mat();

        var ok = saliency.Compute(_mat, saliencyMap);
        if (!ok) throw new ComputeSaliencyFailedException(FilePath);

        using var outMap = new Mat();
        saliencyMap.ConvertTo(outMap, DepthType.Cv32F);
        CvInvoke.Normalize(outMap, outMap, 0, 1, NormType.MinMax);

        return outMap.Clone();
    }

    /// <summary>
    /// Finds the best crop position based on saliency (prominent area).
    /// Uses median of saliency values in each possible crop region.
    /// </summary>
    /// <param name="size">Target crop size.</param>
    /// <returns>Rectangle of the best crop region and its median saliency value.</returns>
    public (Rectangle Roi, double SaliencyValue) GetProminentCrop(Size size)
    {
        using var saliencyMap = ComputeSaliency();

        var h = _mat.Height;
        var w = _mat.Width;

        var cropW = Math.Min(w, size.Width);
        var cropH = Math.Min(h, size.Height);

        using var medianMap = new Mat();
        var kernelSize = Math.Max(cropW, cropH);
        // MedianBlur requires odd kernel size
        if (kernelSize % 2 == 0) kernelSize++;

        // Convert to 8-bit for MedianBlur (required by OpenCV)
        using var saliency8U = new Mat();
        saliencyMap.ConvertTo(saliency8U, DepthType.Cv8U, 255.0);

        CvInvoke.MedianBlur(saliency8U, medianMap, kernelSize);

        // Find maximum location (highest median saliency)
        double minVal = 0, maxVal = 0;
        Point minLoc = default, maxLoc = default;
        CvInvoke.MinMaxLoc(medianMap, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

        // Calculate top-left corner from center point
        var topLeftX = maxLoc.X - cropW / 2;
        var topLeftY = maxLoc.Y - cropH / 2;

        // Clamp to valid range
        topLeftX = Math.Clamp(topLeftX, 0, w - cropW);
        topLeftY = Math.Clamp(topLeftY, 0, h - cropH);

        return (new Rectangle(topLeftX, topLeftY, cropW, cropH), maxVal / 255.0);
    }

    /// <summary>
    /// Calculates the center crop coordinates.
    /// </summary>
    public Rectangle GetCenterCrop(Size size)
    {
        var point = new Point
        {
            X = Math.Max(0, (Width - size.Width) / 2),
            Y = Math.Max(0, (Height - size.Height) / 2)
        };
        return new Rectangle(point, size);
    }

    /// <summary>
    /// Crops the image to specified position and size.
    /// </summary>
    public void Crop(Rectangle roi)
    {
        var cropped = new Mat(_mat, roi);

        _mat.Dispose();
        _mat = cropped.Clone();
        cropped.Dispose();
    }

    /// <summary>
    /// Saves the current image to the specified file path using ImageMagick.
    /// </summary>
    public void Save(string? outputPath = null)
    {
        outputPath ??= FilePath;

        // Convert Mat to bytes
        var dataSize = _mat.Width * _mat.Height * _mat.NumberOfChannels;
        var bytes = new byte[dataSize];
        Marshal.Copy(_mat.DataPointer, bytes, 0, dataSize);

        // Create MagickImage from bytes
        var readSettings = new MagickReadSettings
        {
            Width = (uint)_mat.Width,
            Height = (uint)_mat.Height,
            Format = MagickFormat.Bgr
        };

        using var magickImage = new MagickImage(bytes, readSettings);
        magickImage.Write(outputPath);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mat.Dispose();
        GC.SuppressFinalize(this);
    }
}