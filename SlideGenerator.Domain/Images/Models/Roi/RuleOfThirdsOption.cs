using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Domain.Images.Models.Roi;

/// <summary>
///     Runtime options for rule-of-thirds ROI.
/// </summary>
public sealed record RuleOfThirdsOption : RoiOption
{
    /// <inheritdoc />
    public override RoiType Type => RoiType.RuleOfThirds;

    /// <summary>
    ///     Initializes a new rule-of-thirds option with default upper-third eye-line pin.
    /// </summary>
    [SetsRequiredMembers]
    public RuleOfThirdsOption()
    {
        Pivot = new Vector2(1 / 2f, 1 / 3f);
    }
}


