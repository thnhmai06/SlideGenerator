using System.Drawing;
using SlideGenerator.Domain.Image.Abstractions;
using SlideGenerator.Domain.Image.Entities;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Image.Entities;

/// Reviewed by @thnhmai06 at 01/03/2026 02:09:00 GMT+7
public sealed class CenterRoi : RoiCalculator
{
    /// <summary>
    ///     Calculates the center crop coordinates.
    /// </summary>
    /// <param name="mat">The source mat.</param>
    /// <param name="size">The desired crop size.</param>
    /// <returns>A centered rectangle of the requested size (clamped to image bounds).</returns>
    public static Rectangle GetCenterRoi(IMat mat, Size size)
    {
        var point = new Point
        {
            X = Math.Max(0, (mat.Width - size.Width) / 2),
            Y = Math.Max(0, (mat.Height - size.Height) / 2)
        };
        return new Rectangle(point, size);
    }

    /// <summary>
    ///     Calculates the center crop coordinates asynchronously.
    /// </summary>
    /// <param name="mat">The source mat.</param>
    /// <param name="targetSize">The desired crop size.</param>
    /// <returns>A centered rectangle of the requested size (clamped to image bounds).</returns>
    public override ValueTask<Rectangle> CalculateRoiAsync(IMat mat, Size targetSize)
    {
        return ValueTask.FromResult(GetCenterRoi(mat, targetSize));
    }
}