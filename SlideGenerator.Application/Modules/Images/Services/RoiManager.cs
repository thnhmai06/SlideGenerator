using System.Drawing;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Application.Modules.Images.Services;

/// <summary>
///     Routes region of interest (ROI) calculation requests to the appropriate configured <see cref="IRoiCalculator" />.
/// </summary>
/// <param name="roiCalculators">
///     A dictionary mapping <see cref="RoiType" /> keys to their concrete
///     <see cref="IRoiCalculator" /> implementations.
/// </param>
public sealed class RoiManager(IReadOnlyDictionary<RoiType, IRoiCalculator> roiCalculators) : IRoiCalculator
{
    /// <inheritdoc />
    public ValueTask<Rectangle> CalculateRoiAsync(
        IImage mat, Size targetSize, RoiType type,
        RoiOption? roiOption = null)
    {
        return GetCalculator(type).CalculateRoiAsync(mat, targetSize, type, roiOption);
    }

    /// <summary>
    ///     Retrieves the correct calculator based on the provided ROI type.
    /// </summary>
    /// <param name="key">The <see cref="RoiType" /> enum key.</param>
    /// <returns>The mapped <see cref="IRoiCalculator" />.</returns>
    /// <exception cref="NotSupportedException">Thrown when no calculator is registered for the specified key.</exception>
    private IRoiCalculator GetCalculator(RoiType key)
    {
        return roiCalculators.TryGetValue(key, out var calculator)
            ? calculator
            : throw new NotSupportedException($"ROI calculator '{key}' is not supported.");
    }
}