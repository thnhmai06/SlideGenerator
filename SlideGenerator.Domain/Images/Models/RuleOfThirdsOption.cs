using System.Numerics;

namespace SlideGenerator.Domain.Images.Models;

/// <summary>
///     Runtime options for rule-of-thirds ROI.
/// </summary>
public sealed record RuleOfThirdsOption : RoiOption
{
    /// <summary>
    ///     Initializes a new rule-of-thirds option with default upper-third eye-line pin.
    /// </summary>
    public RuleOfThirdsOption()
    {
        Pivot = new Vector2(1 / 2f, 1 / 3f);
    }
}