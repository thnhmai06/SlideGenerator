using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using ImageMagick;
using OpenCvSharp;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Images;

/// <summary>
///     Provides utility methods for image processing, geometric calculations, and format conversions.
/// </summary>
/// <remarks>
///     This utility class facilitates operations between ImageMagick and OpenCV formats,
///     providing seamless conversion and processing capabilities for image manipulation.
/// </remarks>
public static class Utilities
{
    /// <summary>
    ///     Clamps the specified point so that its coordinates lie within the bounds of the given rectangle.
    /// </summary>
    public static Point ClampIn(this Point point, Rectangle border)
    {
        var x = Math.Clamp(point.X, border.Left, border.Right - 1);
        var y = Math.Clamp(point.Y, border.Top, border.Bottom - 1);

        return new Point(x, y);
    }

    /// <summary>
    ///     Calculates an anchored rectangle of the specified size inside the source image.
    /// </summary>
    public static Rectangle CalculateAnchoredRectangle(
        Size sourceSize, Size cropSize,
        Point? anchorPoint = null, Vector2? pivot = null)
    {
        anchorPoint ??= sourceSize.CenterPoint();
        pivot ??= new Vector2(0.5f, 0.5f);

        var imageBounds = new Rectangle(Point.Empty, sourceSize);
        var boundedSize = new Size(
            Math.Min(cropSize.Width, imageBounds.Width),
            Math.Min(cropSize.Height, imageBounds.Height));

        var x = (int)MathF.Round(anchorPoint.Value.X - boundedSize.Width * pivot.Value.X);
        var y = (int)MathF.Round(anchorPoint.Value.Y - boundedSize.Height * pivot.Value.Y);

        return new Rectangle(x, y, boundedSize.Width, boundedSize.Height).ClampIn(imageBounds);
    }

    /// <summary>
    ///     Converts a <see cref="Point" /> to a <see cref="Vector2" />.
    /// </summary>
    public static Vector2 ToVector2(this Point point)
    {
        return new Vector2(point.X, point.Y);
    }

    /// <summary>
    ///     Converts a <see cref="Vector2" /> to a <see cref="Point" />.
    /// </summary>
    public static Point ToPoint(this Vector2 vector)
    {
        return new Point(
            (int)MathF.Round(vector.X, MidpointRounding.AwayFromZero),
            (int)MathF.Round(vector.Y, MidpointRounding.AwayFromZero)
        );
    }

    /// <summary>
    ///     Calculates the centroid of a collection of points.
    /// </summary>
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

        var sum = sources.Aggregate(Vector2.Zero, (acc, v) => acc + v);
        return (sum / sources.Count).ToPoint();
    }

    extension(Rectangle rect)
    {
        /// <summary>
        ///     Linearly interpolates a point within the rectangle based on a pivot.
        /// </summary>
        public Point Lerp(Vector2 pivot)
        {
            return new Point(
                rect.X + (int)Math.Round(rect.Width * pivot.X, MidpointRounding.AwayFromZero),
                rect.Y + (int)Math.Round(rect.Height * pivot.Y, MidpointRounding.AwayFromZero)
            );
        }

        /// <summary>
        ///     Clamps the specified rectangle so that it fits entirely within the bounds of the given border rectangle.
        /// </summary>
        public Rectangle ClampIn(Rectangle border)
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

        public MagickGeometry ToMagick()
        {
            return new MagickGeometry(
                Math.Max(0, rect.X),
                Math.Max(0, rect.Y),
                (uint)Math.Max(1, rect.Width),
                (uint)Math.Max(1, rect.Height));
        }
    }

    extension(Size original)
    {
        /// <summary>
        ///     Get the largest size that has the same aspect ratio with the target size and fits within the original size.
        /// </summary>
        public Size GetMaxAspectSize(Size ratioSize)
        {
            var originalAspect = original.Width / (double)original.Height;
            var ratioAspect = ratioSize.Width / (double)ratioSize.Height;

            int width, height;
            if (originalAspect >= ratioAspect)
            {
                height = original.Height;
                width = (int)Math.Round(height * ratioAspect);
            }
            else
            {
                width = original.Width;
                height = (int)Math.Round(width / ratioAspect);
            }

            width = Math.Min(width, original.Width);
            height = Math.Min(height, original.Height);
            return new Size(width, height);
        }

        /// <summary>
        ///     Gets the center point of the size.
        /// </summary>
        public Point CenterPoint()
        {
            return new Point(original.Width / 2, original.Height / 2);
        }

        /// <summary>
        ///     Linearly interpolates a point within the size based on a pivot.
        /// </summary>
        public Point Lerp(Vector2 interpolateWith)
        {
            return new Point(
                (int)Math.Round(original.Width * interpolateWith.X, MidpointRounding.AwayFromZero),
                (int)Math.Round(original.Height * interpolateWith.Y, MidpointRounding.AwayFromZero)
            );
        }
    }

    /// <summary>
    ///     Converts Image to OpenCV Mat for face detection operations.
    /// </summary>
    public static Mat ToMat(this MagickImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        image.Format = MagickFormat.Png;
        var pngBytes = image.ToByteArray();

        var mat = Cv2.ImDecode(pngBytes, ImreadModes.Unchanged);
        if (mat.Empty())
        {
            mat.Dispose();
            throw new InvalidOperationException("Cannot decode image bytes into OpenCV Mat.");
        }

        return mat;
    }

    /// <summary>
    ///     Converts OpenCV Mat to Image.
    /// </summary>
    public static MagickImage ToMagick(this Mat mat)
    {
        if (mat.Empty()) return new MagickImage();
        
        var bytes = new byte[mat.Total() * mat.ElemSize()];
        Marshal.Copy(mat.Data, bytes, 0, bytes.Length);
        return new MagickImage(bytes, new MagickReadSettings
        {
            Width = (uint)mat.Width,
            Height = (uint)mat.Height,
            Depth = 8,
            ColorSpace = ColorSpace.RGB
        });
    }
}
