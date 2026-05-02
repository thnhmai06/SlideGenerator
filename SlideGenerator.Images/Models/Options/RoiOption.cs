using System.Numerics;

namespace SlideGenerator.Images.Models.Options;

/// <summary>
///     Provides customization options for ROI calculation.
/// </summary>
public abstract class RoiOption
{
    /// <summary>
    ///     Gets or sets the target pivot point (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    ///     Determines where the anchor point (e.g., face, grid point) is positioned within the resulting ROI.
    /// </remarks>
    public Vector2 Pivot { get; init; } = new(1 / 2f, 1 / 2f);

    /// <summary>
    ///     Gets or sets the fingerprinting option.
    /// </summary>
    public bool UseFingerprint { get; init; }

    /// <summary>
    ///     Gets the region of interest (ROI) detection type.
    /// </summary>
    public abstract RoiType Type { get; }
}