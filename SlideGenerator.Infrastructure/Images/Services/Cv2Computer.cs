using OpenCvSharp;
using SlideGenerator.Application.Images.Abstractions;
using SlideGenerator.Domain.Images.Abstractions;
using Mat = SlideGenerator.Infrastructure.Images.Adapters.Mat;
using Point = System.Drawing.Point;

namespace SlideGenerator.Infrastructure.Images.Services;

public class Cv2Computer : IVisionComputer
{
    public IMat? ComputeSaliency(IMat image)
    {
        if (image.Empty()) return null;
        var mat = (Mat)image;

        try
        {
            // Apply Laplacian as a simple saliency detector (alternative to spectral residual)
            using var laplacian = new OpenCvSharp.Mat();
            Cv2.Laplacian(mat.Core, laplacian, MatType.CV_32F, 5);

            // Get absolute values
            using var absLaplacian = Cv2.Abs(laplacian).ToMat();

            // Normalize to 0-1 range
            var normalized = new OpenCvSharp.Mat();
            Cv2.Normalize(absLaplacian, normalized, 0, 1, NormTypes.MinMax);
            return new Mat(normalized);
        }
        catch
        {
            return null;
        }
    }

    public Point? ComputeVisualCenter(IMat image)
    {
        if (image.Empty()) return null;
        var mat = ((Mat)image).Core;

        // grayscale
        using var gray = new OpenCvSharp.Mat();
        if (mat.Channels() > 1)
        {
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            mat.CopyTo(gray);
        }

        // binary
        using var binary = new OpenCvSharp.Mat();
        Cv2.Threshold(gray, binary, 127, 255, ThresholdTypes.Binary);
        if (Cv2.CountNonZero(binary) == 0)
            return null;

        // distance transform
        using var distTransform = new OpenCvSharp.Mat();
        Cv2.DistanceTransform(binary, distTransform, DistanceTypes.L2, DistanceTransformMasks.Precise);
        
        Cv2.MinMaxLoc(distTransform, out _, out _, out _, out var maxLoc);
        return new Point(maxLoc.X, maxLoc.Y);
    }
}