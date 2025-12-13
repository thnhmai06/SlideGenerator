using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Saliency;
using System.Drawing;
using TaoSlideTotNghiep.Domain.Image.Enums;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Image;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Image;

internal static class ImageEngine
{
    public static Rectangle GetRoi(Models.Image image, ImageRoiType roiType, Size size)
    {
        return roiType switch
        {
            ImageRoiType.Prominent => GetProminentRoi(image, size).Roi,
            ImageRoiType.Center => GetCenterRoi(image, size),
            _ => throw new ArgumentOutOfRangeException(nameof(roiType), roiType, null)
        };
    }

    /// <summary>
    /// Crops the image to specified position and size.
    /// </summary>
    public static void Crop(Models.Image image, Rectangle roi)
    {
        var cropped = new Mat(image.Mat, roi);

        image.Mat.Dispose();
        image.Mat = cropped.Clone();
        cropped.Dispose();
    }

    /// <summary>
    /// Computes a normalized saliency map for the specified image using the spectral residual method.
    /// </summary>
    /// <remarks>The returned saliency map highlights regions of the image that are likely to attract human
    /// attention. The normalization ensures that the output values are suitable for further processing or
    /// visualization.</remarks>
    /// <param name="image">The image for which to compute the saliency map. Must not be null and should contain valid image data.</param>
    /// <returns>A new Mat instance containing the normalized saliency map, with pixel values scaled between 0 and 1.</returns>
    /// <exception cref="ComputeSaliencyFailedException">Thrown if the saliency computation fails for the provided image.</exception>
    private static Mat ComputeSaliency(Models.Image image)
    {
        using var saliency = new StaticSaliencySpectralResidual();
        using var saliencyMap = new Mat();

        var ok = saliency.Compute(image.Mat, saliencyMap);
        if (!ok) throw new ComputeSaliencyFailedException(image.FilePath);

        using var outMap = new Mat();
        saliencyMap.ConvertTo(outMap, DepthType.Cv32F);
        CvInvoke.Normalize(outMap, outMap, 0, 1, NormType.MinMax);

        return outMap.Clone();
    }

    /// <summary>
    /// Finds the best crop position based on saliency (prominent area).
    /// Uses median of saliency values in each possible crop region.
    /// </summary>
    /// <param name="image">Image that want to get.</param>
    /// <param name="size">Target crop size.</param>
    /// <returns>Rectangle of the best crop region and its median saliency value.</returns>
    private static (Rectangle Roi, double SaliencyValue) GetProminentRoi(Models.Image image, Size size)
    {
        using var saliencyMap = ComputeSaliency(image);

        var h = image.Mat.Height;
        var w = image.Mat.Width;

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
    private static Rectangle GetCenterRoi(Models.Image image, Size size)
    {
        var point = new Point
        {
            X = Math.Max(0, (image.Mat.Width - size.Width) / 2),
            Y = Math.Max(0, (image.Mat.Height - size.Height) / 2)
        };
        return new Rectangle(point, size);
    }
}