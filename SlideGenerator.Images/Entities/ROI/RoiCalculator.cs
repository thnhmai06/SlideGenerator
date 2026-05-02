using System.Drawing;
using SlideGenerator.Images.Models;
using SlideGenerator.Images.Models.Options;
using Mat = OpenCvSharp.Mat;

namespace SlideGenerator.Images.Entities.ROI;

internal abstract class RoiCalculator
{
    /// <summary>
    ///     Calculates ROI by routing to the calculator keyed by the option type.
    /// </summary>
    /// <param name="mat">The source image mat.</param>
    /// <param name="targetSize">The desired ROI size.</param>
    /// <param name="type">The ROI type used to resolve the target calculator.</param>
    /// <param name="option">The customized ROI option used to resolve the target calculator.</param>
    /// <returns>The calculated ROI rectangle.</returns>
    public abstract ValueTask<Rectangle> CalculateRoiAsync(
        Mat mat, Size targetSize, RoiType type,
        RoiOption? option = null);
}