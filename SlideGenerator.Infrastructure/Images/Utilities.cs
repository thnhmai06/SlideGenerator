using System.Drawing;
using OpenCvSharp;
using CvSize = OpenCvSharp.Size;

namespace SlideGenerator.Infrastructure.Images;

public static class Utilities
{
    /// <summary>
    ///     Crops the specified mat to the given dimensions in place.
    /// </summary>
    /// <param name="mat">The mat to crop (modified in place).</param>
    /// <param name="rect">The region of interest to crop to.</param>
    public static void Crop(ref Mat mat, Rectangle rect)
    {
        var croppedMat = new Mat(mat, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        var cloned = croppedMat.Clone();

        mat.Dispose();
        mat = cloned;
        croppedMat.Dispose();
    }

    /// <summary>
    ///     Resizes the specified mat to the given dimensions in place.
    /// </summary>
    /// <param name="mat">The mat to resize (modified in place).</param>
    /// <param name="size">The size to resize to.</param>
    /// <param name="interpolation">Interpolation method.</param>
    public static void Resize(ref Mat mat, CvSize size, InterpolationFlags interpolation = InterpolationFlags.Area)
    {
        var resizedMat = new Mat();
        Cv2.Resize(mat, resizedMat, size, 0, 0, interpolation);

        mat.Dispose();
        mat = resizedMat;
    }
    
    /// <summary>
    ///     Get the largest size that has the same aspect ratio with the target size and fits within the original size.
    /// </summary>
    /// <param name="original">The original size (OpenCvSharp.Size).</param>
    /// <param name="target">The target size (OpenCvSharp.Size).</param>
    /// <returns>The largest size that has the same aspect ratio with the target size and fits within the original size.</returns>
    public static CvSize GetMaxAspectSize(this CvSize original, CvSize target)
    {
        var originalAspect = original.Width / (double)original.Height;
        var targetAspect = target.Width / (double)target.Height;

        int width, height;
        if (originalAspect >= targetAspect)
        {
            height = original.Height;
            width = (int)Math.Round(height * targetAspect);
        }
        else
        {
            width = original.Width;
            height = (int)Math.Round(width / targetAspect);
        }

        width = Math.Min(width, original.Width);
        height = Math.Min(height, original.Height);
        return new CvSize(width, height);
    }
}