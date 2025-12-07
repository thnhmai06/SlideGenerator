using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageMagick;
using TaoSlideTotNghiep.Exceptions;

namespace TaoSlideTotNghiep.Logic;

/// <summary>
/// Represents an image.
/// Provides methods for saliency detection and cropping.
/// </summary>
public class Image : IDisposable
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
        // Convert to grayscale for saliency computation
        using var grayImage = new Mat();
        CvInvoke.CvtColor(_mat, grayImage, ColorConversion.Bgr2Gray);

        // Use Spectral Residual Saliency (manual implementation)
        // Step 1: FFT
        using var floatImage = new Mat();
        grayImage.ConvertTo(floatImage, DepthType.Cv32F);

        using var padded = new Mat();
        var optimalRows = CvInvoke.GetOptimalDFTSize(floatImage.Rows);
        var optimalCols = CvInvoke.GetOptimalDFTSize(floatImage.Cols);
        CvInvoke.CopyMakeBorder(floatImage, padded, 0, optimalRows - floatImage.Rows, 
            0, optimalCols - floatImage.Cols, BorderType.Constant, new MCvScalar(0));

        // Create complex planes
        using var planes = new VectorOfMat();
        planes.Push(padded);
        using var zeros = Mat.Zeros(padded.Rows, padded.Cols, DepthType.Cv32F, 1);
        planes.Push(zeros);

        using var complexImage = new Mat();
        CvInvoke.Merge(planes, complexImage);

        // DFT
        using var dftResult = new Mat();
        CvInvoke.Dft(complexImage, dftResult);

        // Split into magnitude and phase
        using var splitPlanes = new VectorOfMat();
        CvInvoke.Split(dftResult, splitPlanes);

        using var magnitude = new Mat();
        using var phase = new Mat();
        CvInvoke.CartToPolar(splitPlanes[0], splitPlanes[1], magnitude, phase);

        // Log magnitude
        using var logMagnitude = new Mat();
        using var ones = new Mat(magnitude.Size, DepthType.Cv32F, 1);
        ones.SetTo(new MCvScalar(1));
        CvInvoke.Add(magnitude, ones, logMagnitude);
        CvInvoke.Log(logMagnitude, logMagnitude);

        // Spectral Residual = LogMagnitude - AverageFilter(LogMagnitude)
        using var avgFilter = new Mat();
        CvInvoke.Blur(logMagnitude, avgFilter, new Size(3, 3), new Point(-1, -1));

        using var spectralResidual = new Mat();
        CvInvoke.Subtract(logMagnitude, avgFilter, spectralResidual);

        // Reconstruct using exp(spectralResidual) and original phase
        using var expResidual = new Mat();
        CvInvoke.Exp(spectralResidual, expResidual);

        using var realPart = new Mat();
        using var imagPart = new Mat();
        CvInvoke.PolarToCart(expResidual, phase, realPart, imagPart);

        using var reconstructPlanes = new VectorOfMat();
        reconstructPlanes.Push(realPart);
        reconstructPlanes.Push(imagPart);

        using var reconstructComplex = new Mat();
        CvInvoke.Merge(reconstructPlanes, reconstructComplex);

        // Inverse DFT
        using var idftResult = new Mat();
        CvInvoke.Dft(reconstructComplex, idftResult, DxtType.Inverse | DxtType.Scale);

        using var idftPlanes = new VectorOfMat();
        CvInvoke.Split(idftResult, idftPlanes);

        // Compute magnitude of result: sqrt(real^2 + imag^2)
        using var realSquared = new Mat();
        using var imagSquared = new Mat();
        CvInvoke.Multiply(idftPlanes[0], idftPlanes[0], realSquared);
        CvInvoke.Multiply(idftPlanes[1], idftPlanes[1], imagSquared);
        
        using var sumSquares = new Mat();
        CvInvoke.Add(realSquared, imagSquared, sumSquares);
        
        using var saliencyTemp = new Mat();
        CvInvoke.Sqrt(sumSquares, saliencyTemp);

        // Square the magnitude for emphasis
        using var saliencySquared = new Mat();
        CvInvoke.Multiply(saliencyTemp, saliencyTemp, saliencySquared);

        // Gaussian blur
        var saliencyMap = new Mat();
        CvInvoke.GaussianBlur(saliencySquared, saliencyMap, new Size(11, 11), 2.5);

        // Normalize to 0-1
        CvInvoke.Normalize(saliencyMap, saliencyMap, 0, 1, NormType.MinMax);

        // Crop to original size
        var roi = new Rectangle(0, 0, _mat.Width, _mat.Height);
        var croppedSaliency = new Mat(saliencyMap, roi);

        return croppedSaliency.Clone();
    }

    /// <summary>
    /// Finds the best crop position based on saliency (prominent area).
    /// </summary>
    /// <param name="targetWidth">Target crop width.</param>
    /// <param name="targetHeight">Target crop height.</param>
    /// <returns>Top-left coordinates (x, y) and best saliency value.</returns>
    public (Point TopLeft, double SaliencyValue) GetProminentCrop(int targetWidth, int targetHeight)
    {
        using var saliencyMap = ComputeSaliency();
        
        var h = _mat.Height;
        var w = _mat.Width;

        // Resize if image is smaller than target size
        var ratio = Math.Max(Math.Max((double)targetHeight / h, (double)targetWidth / w), 1.0);
        
        var workingImage = _mat;
        var workingSaliency = saliencyMap;
        var needsDispose = false;

        if (ratio > 1)
        {
            var newWidth = (int)(w * ratio);
            var newHeight = (int)(h * ratio);
            
            workingImage = new Mat();
            CvInvoke.Resize(_mat, workingImage, new Size(newWidth, newHeight), 0, 0, Inter.Area);
            
            workingSaliency = new Mat();
            CvInvoke.Resize(saliencyMap, workingSaliency, new Size(newWidth, newHeight), 0, 0, Inter.Nearest);
            
            w = newWidth;
            h = newHeight;
            needsDispose = true;
        }

        var tW = Math.Min(w, targetWidth);
        var tH = Math.Min(h, targetHeight);

        // Compute score map using box filter
        using var scoreMap = new Mat();
        CvInvoke.BoxFilter(workingSaliency, scoreMap, DepthType.Cv32F, new Size(tW, tH), 
            new Point(-1, -1), true, BorderType.Constant);

        // Find maximum location
        double minVal = 0, maxVal = 0;
        Point minLoc = default, maxLoc = default;
        CvInvoke.MinMaxLoc(scoreMap, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

        // Calculate top-left corner
        var topLeftX = (int)(maxLoc.X - tW / 2.0);
        var topLeftY = (int)(maxLoc.Y - tH / 2.0);

        // Clamp to valid range
        topLeftX = Math.Max(0, Math.Min(topLeftX, w - tW));
        topLeftY = Math.Max(0, Math.Min(topLeftY, h - tH));

        // Cleanup if we created new mats
        if (needsDispose)
        {
            workingImage.Dispose();
            workingSaliency.Dispose();
        }

        return (new Point(topLeftX, topLeftY), maxVal);
    }

    /// <summary>
    /// Calculates the center crop coordinates.
    /// </summary>
    public Point GetCenterCrop(int targetWidth, int targetHeight)
    {
        var topLeftX = Math.Max(0, (Width - targetWidth) / 2);
        var topLeftY = Math.Max(0, (Height - targetHeight) / 2);
        return new Point(topLeftX, topLeftY);
    }

    /// <summary>
    /// Crops the image to specified position and size.
    /// </summary>
    public void Crop(int x, int y, int width, int height)
    {
        var roi = new Rectangle(x, y, width, height);
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
