namespace SlideGenerator.Images.Models.Options;

/// <summary>
///     Customizes the Rule of Thirds ROI calculation.
/// </summary>
public sealed class RuleOfThirdsOption : RoiOption
{
    /// <summary>
    ///     Gets or sets the specific grid point (1-4) to align the anchor to.
    /// </summary>
    /// <remarks>
    ///     Points are numbered 1 (top-left) to 4 (bottom-right) in row-major order.
    /// </remarks>
    public int GridPoint { get; init; } = 1;

    public override RoiType Type => RoiType.RuleOfThirds;
}