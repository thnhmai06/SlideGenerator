using System.Drawing;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models.Roi;

namespace SlideGenerator.Application.Images.Interfaces;

/// <summary>
///     Provides ROI calculation routing based on ROI option type.
/// </summary>
public interface IRoiProvider
{
    /// <summary>
    ///     Calculates ROI by routing to the corresponding calculator for the specified option type.
    /// </summary>
    /// <param name="mat">The source image mat.</param>
    /// <param name="targetSize">The desired ROI size.</param>
    /// <param name="roiOption">The ROI option used to resolve the target calculator.</param>
    /// <returns>The calculated ROI rectangle.</returns>
    ValueTask<Rectangle> CalculateRoiAsync(IMat mat, Size targetSize, RoiOption roiOption);
}