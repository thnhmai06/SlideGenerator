using System.Drawing;
using OpenCvSharp;
using CvSize = OpenCvSharp.Size;

namespace SlideGenerator.Infrastructure.Images;

/// <summary>
///     Provides utility methods for image manipulation using <see cref="OpenCvSharp" />.
/// </summary>
public static class Utilities
{
    /// <summary>
    ///     Crops the specified <see cref="Mat" /> to the given dimensions in place.
    /// </summary>
    /// <param name="mat">
    ///     The <see cref="Mat" /> to crop. The original object is disposed and replaced with the cropped
    ///     version.
    /// </param>
    /// <param name="rect">The <see cref="Rectangle" /> representing the region of interest to crop to.</param>
    public static void Crop(ref Mat mat, Rectangle rect)
    {
        var croppedMat = new Mat(mat, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        var cloned = croppedMat.Clone();

        mat.Dispose();
        mat = cloned;
        croppedMat.Dispose();
    }

    /// <summary>
    ///     Resizes the specified <see cref="Mat" /> to the given dimensions in place.
    /// </summary>
    /// <param name="mat">
    ///     The <see cref="Mat" /> to resize. The original object is disposed and replaced with the resized
    ///     version.
    /// </param>
    /// <param name="size">The target <see cref="CvSize" /> to resize to.</param>
    /// <param name="interpolation">
    ///     The <see cref="InterpolationFlags" /> method to use for resizing. Defaults to
    ///     <see cref="InterpolationFlags.Area" />.
    /// </param>
    public static void Resize(ref Mat mat, CvSize size, InterpolationFlags interpolation = InterpolationFlags.Area)
    {
        var resizedMat = new Mat();
        Cv2.Resize(mat, resizedMat, size, 0, 0, interpolation);

        mat.Dispose();
        mat = resizedMat;
    }

    /// <summary>
    ///     Calculates the largest <see cref="CvSize" /> that maintains the aspect ratio of the target size while fitting
    ///     within the original size.
    /// </summary>
    /// <param name="original">The original <see cref="CvSize" /> to fit within.</param>
    /// <param name="target">The target <see cref="CvSize" /> whose aspect ratio should be maintained.</param>
    /// <returns>
    ///     A <see cref="CvSize" /> representing the largest possible dimensions that fit within
    ///     <paramref name="original" /> with <paramref name="target" />'s aspect ratio.
    /// </returns>
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