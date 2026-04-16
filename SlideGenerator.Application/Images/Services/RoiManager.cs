using System.Drawing;
using SlideGenerator.Application.Images.Entities;
using SlideGenerator.Application.Images.Interfaces;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models.Roi;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Application.Images.Services;

/// <summary>
///     Routes ROI calculation requests to singleton ROI calculators by ROI type.
/// </summary>
public sealed class RoiManager(CenterRoi centerRoi, RuleOfThirdsRoi ruleOfThirdsRoi) : IRoiProvider
{
    /// <inheritdoc />
    public ValueTask<Rectangle> CalculateRoiAsync(IMat mat, Size targetSize, RoiOption roiOption)
    {
        return roiOption.Type switch
        {
            RoiType.Center => centerRoi.CalculateRoiAsync(mat, targetSize, roiOption),
            RoiType.RuleOfThirds => ruleOfThirdsRoi.CalculateRoiAsync(mat, targetSize, roiOption),
            _ => throw new NotSupportedException($"ROI calculator '{roiOption.Type}' is not supported.")
        };
    }
}