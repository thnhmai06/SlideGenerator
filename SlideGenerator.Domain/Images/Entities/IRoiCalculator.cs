using System.Drawing;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;
using Size = System.Drawing.Size;

namespace SlideGenerator.Domain.Images.Entities;

/// Reviewed by @thnhmai06 at 15/04/2026 14:49:59 GMT+7
public interface IRoiCalculator
{
    /// <summary>
    ///     Calculates ROI by routing to the calculator keyed by the option type.
    /// </summary>
    /// <param name="mat">The source image mat.</param>
    /// <param name="targetSize">The desired ROI size.</param>
    /// <param name="type">The ROI type used to resolve the target calculator.</param>
    /// <param name="options">The customized ROI option used to resolve the target calculator.</param>
    /// <returns>The calculated ROI rectangle.</returns>
    ValueTask<Rectangle> CalculateRoiAsync(
        IImage mat, Size targetSize, RoiType type,
        RoiOption? options = null);
}