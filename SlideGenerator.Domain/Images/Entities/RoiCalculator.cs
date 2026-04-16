using System.Drawing;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models.Roi;
using SlideGenerator.Domain.Images.Rules;
using Size = System.Drawing.Size;

namespace SlideGenerator.Domain.Images.Entities;

/// Reviewed by @thnhmai06 at 15/04/2026 14:49:59 GMT+7
public abstract class RoiCalculator
{
    /// <summary>
    ///     Gets the ROI calculator type.
    /// </summary>
    public abstract RoiType Type { get; }

    /// <summary>
    ///     Asynchronously calculates the region of interest (ROI) within the specified image that best matches the given
    ///     target size, with optional runtime configuration.
    /// </summary>
    /// <param name="mat">The source mat in which to search for the region of interest. Cannot be null.</param>
    /// <param name="targetSize">The desired size of the region of interest to locate within the image, in pixels.</param>
    /// <param name="options">
    ///     Optional runtime calculation option for this ROI execution. If null, default configuration is
    ///     used.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a Rectangle specifying the
    ///     coordinates and size of the detected region of interest.
    /// </returns>
    public abstract ValueTask<Rectangle> CalculateRoiAsync(IMat mat, Size targetSize, RoiOption? options = null);
}