using System.Drawing;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Image;

/// <summary>
///     Provides static methods for manipulating images and geometric shapes, including cropping, resizing, and clamping
///     rectangles and points to specified bounds.
/// </summary>
/// Reviewed by @thnhmai06 at 05/03/2026
public static class Utilities
{
    /// <summary>
    ///     Clamps the specified rectangle so that it fits entirely within the bounds of the given border rectangle.
    /// </summary>
    /// <remarks>
    ///     If the input rectangle is larger than the border in either dimension, the returned rectangle
    ///     will be resized to match the border's corresponding dimension. The position is adjusted as needed to ensure the
    ///     rectangle remains fully within the border.
    /// </remarks>
    /// <param name="rect">The rectangle to be clamped within the border.</param>
    /// <param name="border">
    ///     The rectangle that defines the border limits. The returned rectangle will not extend outside this
    ///     area.
    /// </param>
    /// <returns>
    ///     A rectangle with the same size as the input rectangle, unless it exceeds the border's dimensions, in which case
    ///     its size and position are adjusted to fit entirely within the border.
    /// </returns>
    public static Rectangle ClampIn(this Rectangle rect, Rectangle border)
    {
        var x = rect.X;
        var y = rect.Y;
        var w = rect.Width;
        var h = rect.Height;

        if (w > border.Width)
        {
            w = border.Width;
            x = border.X;
        }

        if (h > border.Height)
        {
            h = border.Height;
            y = border.Y;
        }

        if (x < border.Left) x = border.Left;
        if (y < border.Top) y = border.Top;
        if (x + w > border.Right) x = border.Right - w;
        if (y + h > border.Bottom) y = border.Bottom - h;

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    ///     Clamps the specified point so that its coordinates lie within the bounds of the given rectangle.
    /// </summary>
    /// <remarks>
    ///     If the point's coordinates are already within the rectangle, the original point is returned
    ///     unchanged. The rectangle's right and bottom edges are considered exclusive, matching standard .NET rectangle
    ///     behavior.
    /// </remarks>
    /// <param name="point">The point to clamp to the rectangle's borders.</param>
    /// <param name="border">
    ///     The rectangle that defines the bounding area. The point will be clamped to remain within this
    ///     rectangle.
    /// </param>
    /// <returns>A new point whose X and Y coordinates are constrained to the borders of the specified rectangle.</returns>
    public static Point ClampIn(this Point point, Rectangle border)
    {
        var x = Math.Clamp(point.X, border.Left, border.Right - 1);
        var y = Math.Clamp(point.Y, border.Top, border.Bottom - 1);

        return new Point(x, y);
    }

    /// <summary>
    ///     Get the largest size that has the same aspect ratio with the target size and fits within the original size.
    /// </summary>
    /// <param name="original">The original size.</param>
    /// <param name="target">The target size.</param>
    /// <returns>The largest size that has the same aspect ratio with the target size and fits within the original size.</returns>
    public static Size GetMaxAspectSize(this Size original, Size target)
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
        return new Size(width, height);
    }
}