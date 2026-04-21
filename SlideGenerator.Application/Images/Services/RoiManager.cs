using System.Drawing;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Application.Images.Services;

/// <summary>
///     Routes ROI calculation requests to keyed ROI calculators.
/// </summary>
public sealed class RoiManager(IReadOnlyDictionary<RoiType, IRoiCalculator> roiCalculators) : IRoiCalculator
{
    /// <inheritdoc />
    public ValueTask<Rectangle> CalculateRoiAsync(
        IImage mat, Size targetSize, RoiType type,
        RoiOption? roiOption = null)
    {
        return GetCalculator(type).CalculateRoiAsync(mat, targetSize, type, roiOption);
    }

    private IRoiCalculator GetCalculator(RoiType key)
    {
        return roiCalculators.TryGetValue(key, out var calculator)
            ? calculator
            : throw new NotSupportedException($"ROI calculator '{key}' is not supported.");
    }
}