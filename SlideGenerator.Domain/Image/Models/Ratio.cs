using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace SlideGenerator.Domain.Image.Models;

/// <summary>
///     Represents ratios of a rectangle.
/// </summary>
/// Reviewed by @thnhmai06 at 01/03/2026 02:08:30 GMT+7
public struct Ratio
{
    /// <summary>
    ///     Initializes an ratio.
    /// </summary>
    public Ratio()
    {
    }

    /// <summary>
    ///     Initializes a ratio that applies the same margin to all sides.
    /// </summary>
    /// <param name="allSides">
    ///     The margin ratio applied uniformly to the left, top, right, and bottom sides.
    /// </param>
    [SetsRequiredMembers]
    public Ratio(float allSides) : this(allSides, allSides)
    {
    }

    /// <summary>
    ///     Initializes a ratio that applies the same vertical and horizontal margins.
    /// </summary>
    /// <param name="vertical">
    ///     The margin ratio applied to the top and bottom sides.
    /// </param>
    /// <param name="horizontal">
    ///     The margin ratio applied to the left and right sides.
    /// </param>
    [SetsRequiredMembers]
    public Ratio(float vertical, float horizontal)
    {
        Top = Bottom = vertical;
        Left = Right = horizontal;
    }

    /// <summary>
    ///     The margin ratio applied to the top side. (0 to 1)
    /// </summary>
    public required float Top
    {
        get;
        init
        {
            field = value;
            Math.Clamp(value, 0, 1);
        }
    }

    /// <summary>
    ///     The margin ratio applied to the bottom side. (0 to 1)
    /// </summary>
    public required float Bottom
    {
        get;
        init
        {
            field = value;
            Math.Clamp(value, 0, 1);
        }
    }

    /// <summary>
    ///     The margin ratio applied to the left side. (0 to 1)
    /// </summary>
    public required float Left
    {
        get;
        init
        {
            field = value;
            Math.Clamp(value, 0, 1);
        }
    }

    /// <summary>
    ///     The margin ratio applied to the right side. (0 to 1)
    /// </summary>
    public required float Right
    {
        get;
        init
        {
            field = value;
            Math.Clamp(value, 0, 1);
        }
    }


    /// <summary>
    ///     Returns a new rectangle by applying the margin ratios to the given rectangle.
    ///     If a border is provided, the result is shifted to stay within the border
    ///     while preserving its size whenever possible.
    /// </summary>
    /// <remarks>
    ///     Margins are specified as fractions of the rectangle's width and height.
    ///     Positive values expand the rectangle outward, while negative values shrink it inward.
    ///     The input rectangle is not modified.
    /// </remarks>
    /// <param name="original">The source rectangle.</param>
    /// <param name="border">
    ///     Optional bounding rectangle used to constrain the result
    ///     (typically the image bounds).
    /// </param>
    /// <returns>
    ///     A new rectangle adjusted by the margin ratios and constrained to the border.
    /// </returns>
    public Rectangle Expand(Rectangle original, Rectangle? border = null)
    {
        var expanded = new Rectangle(
            original.X - (int)(original.Width * Left),
            original.Y - (int)(original.Height * Top),
            original.Width + (int)(original.Width * (Left + Right)),
            original.Height + (int)(original.Height * (Top + Bottom))
        );
        if (border is null) return expanded;

        var b = border.Value;

        var x = expanded.X;
        var y = expanded.Y;
        var w = expanded.Width;
        var h = expanded.Height;

        // expanded > border => shrink
        if (w > b.Width)
        {
            w = b.Width;
            x = b.X;
        }

        if (h > b.Height)
        {
            h = b.Height;
            y = b.Y;
        }

        // shift into border
        if (x < b.Left) x = b.Left;
        if (y < b.Top) y = b.Top;
        if (x + w > b.Right) x = b.Right - w;
        if (y + h > b.Bottom) y = b.Bottom - h;

        return new Rectangle(x, y, w, h);
    }
}