using System.Drawing;
using ImageMagick;
using SlideGenerator.Images.Models.Options;

namespace SlideGenerator.Images.Entities.ROI;

internal abstract class RoiCalculator
{
    /// <summary>
    ///     Calculates ROI by routing to the calculator keyed by the option type.
    /// </summary>
    /// <param name="image">The source image.</param>
    /// <param name="targetSize">The desired ROI size.</param>
    /// <param name="option">The customized ROI option used to resolve the target calculator.</param>
    /// <returns>The calculated ROI rectangle.</returns>
    public abstract ValueTask<Rectangle> CalculateRoiAsync(MagickImage image, Size targetSize,
        RoiOption option);
}