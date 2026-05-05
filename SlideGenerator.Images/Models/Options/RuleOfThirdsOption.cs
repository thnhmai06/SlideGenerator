using System.Numerics;

namespace SlideGenerator.Images.Models.Options;

/// <summary>
///     Customizes the Rule of Thirds ROI calculation.
/// </summary>
public sealed record RuleOfThirdsOption : RoiOption
{
    public override RoiType Type => RoiType.RuleOfThirds;

    /// <summary>
    ///     Gets or sets the target pivot point (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    ///     Determines where the anchor point (e.g., face, grid point) is positioned within the resulting ROI.
    /// </remarks>
    public Vector2 Pivot { get; init; } = new(1 / 2f, 1 / 3f);
}
