using System.Numerics;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Domain.Images.Models;

/// <summary>
///     Base ROI option for crop strategy.
/// </summary>
public abstract record RoiOption
{
    /// <summary>
    ///     Gets the relative pin position inside the cropped rectangle.
    ///     (0,0) is top-left and (1,1) is bottom-right.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the value of Pivot is out of range (not between 0 and 1).
    /// </exception>
    public Vector2 Pivot
    {
        get;
        init
        {
            if (value.X is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(Pivot), "Pivot.X should be between 0 and 1.");
            if (value.Y is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(Pivot), "Pivot.Y should be between 0 and 1.");
            field = value;
        }
    } = new(1 / 2f, 1 / 2f);

    /// <summary>
    ///     Gets the region of interest (ROI) detection type for image cropping.
    /// </summary>
    public abstract RoiType Type { get; }
}