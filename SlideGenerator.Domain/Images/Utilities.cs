using System.Drawing;
using System.Numerics;

namespace SlideGenerator.Domain.Images;

public static class Utilities
{
    public static Point Lerp(this Size size, Vector2 relativePos)
    {
        return new Point(
            (int)Math.Round(size.Width * relativePos.X, MidpointRounding.AwayFromZero),
            (int)Math.Round(size.Height * relativePos.Y, MidpointRounding.AwayFromZero)
        );
    }

    public static Point Lerp(this Rectangle rect, Vector2 relativePos)
    {
        return new Point(
            rect.X + (int)Math.Round(rect.Width * relativePos.X, MidpointRounding.AwayFromZero),
            rect.Y + (int)Math.Round(rect.Height * relativePos.Y, MidpointRounding.AwayFromZero)
        );
    }
}