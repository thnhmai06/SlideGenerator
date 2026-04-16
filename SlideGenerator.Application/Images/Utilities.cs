using System.Drawing;
using System.Numerics;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Images;

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
    public static Point CenterPoint(this Size size) => new(size.Width / 2, size.Height / 2);

    /// <summary>
    ///     Calculates an anchored rectangle of the specified size inside the source image,
    ///     with the specified anchor point and Pivot.
    /// </summary>
    /// <param name="sourceSize">The source size.</param>
    /// <param name="cropSize">The desired crop size.</param>
    /// <param name="anchorPoint">The point in the source image that should be placed inside the cropped rectangle.</param>
    /// <param name="pivot">The relative position of the anchor point inside the cropped rectangle.</param>
    /// <returns>A rectangle of the requested size, clamped to image bounds.</returns>
    public static Rectangle CalculateAnchoredRectangle(
        Size sourceSize, Size cropSize,
        Point? anchorPoint = null, Vector2? pivot = null)
    {
        anchorPoint ??= sourceSize.CenterPoint();
        pivot ??= new(1 / 2f, 1 / 2f);

        var imageBounds = new Rectangle(Point.Empty, sourceSize);
        var boundedSize = new Size(
            Math.Min(cropSize.Width, imageBounds.Width),
            Math.Min(cropSize.Height, imageBounds.Height));

        var x = (int)MathF.Round(anchorPoint.Value.X - boundedSize.Width * pivot.Value.X);
        var y = (int)MathF.Round(anchorPoint.Value.Y - boundedSize.Height * pivot.Value.Y);

        return new Rectangle(x, y, boundedSize.Width, boundedSize.Height).ClampIn(imageBounds);
    }

    public static Vector2 ToVector2(this Point point) => new(point.X, point.Y);

    public static Point ToPoint(this Vector2 vector) =>
        new(
            (int)MathF.Round(vector.X, MidpointRounding.AwayFromZero),
            (int)MathF.Round(vector.Y, MidpointRounding.AwayFromZero)
        );
    
    public static Point? Centroid<TSource>(
        this IReadOnlyList<TSource> containers,
        Func<TSource, Point?> selector)
    {
        var sources = containers
            .Select(selector)
            .Where(p => p.HasValue)
            .Select(p => p!.Value.ToVector2())
            .ToList();
        if (sources.Count == 0) return null;
        
        var sum  = sources.Aggregate(Vector2.Zero, (acc, v) => acc + v);
        return (sum / containers.Count).ToPoint();
    }
}