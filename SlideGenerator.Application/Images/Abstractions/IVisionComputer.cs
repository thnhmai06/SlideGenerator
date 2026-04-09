using SlideGenerator.Domain.Images.Abstractions;
using Point = System.Drawing.Point;

namespace SlideGenerator.Application.Images.Abstractions;

/// <summary>
///     Provides static methods for computing image saliency maps and determining the visual center of shapes within
///     images.
/// </summary>
/// Reviewed by @thnhmai06 at 01/03/2026 02:11:50 GMT+7
public interface IVisionComputer
{
    /// <summary>
    ///     Computes a normalized saliency map for the specified image using a simple spectral residual-like method.
    /// </summary>
    IMat? ComputeSaliency(IMat image);

    /// <summary>
    ///     Calculates the visual center of a shape represented by a mask image.
    /// </summary>
    Point? ComputeVisualCenter(IMat image);
}