using System.Numerics;

namespace SlideGenerator.Domain.Images.Models;

/// <summary>
///     Runtime options for center ROI.
/// </summary>
public sealed record CenterOption : RoiOption
{
    /// <summary>
    ///     Initializes a new center option with default center pin.
    /// </summary>
    public CenterOption()
    {
        Pivot = new Vector2(1 / 2f, 1 / 2f);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the center ROI should be aligned using detected face centers.
    ///     When <see langword="false" />, the ROI is centered using the image bounds.
    /// </summary>
    public bool UseFaceAlignment { get; init; } = true;
}