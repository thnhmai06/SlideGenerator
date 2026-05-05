using System.Collections.ObjectModel;
using System.Drawing;
using ImageMagick;
using Microsoft.Extensions.Logging;
using SlideGenerator.Images.Entities.Detectors;
using SlideGenerator.Images.Entities.ROI;
using SlideGenerator.Images.Models;
using SlideGenerator.Images.Models.Options;

namespace SlideGenerator.Images.Services;

/// <summary>
///     Resolves Region of Interest (ROI) for images using configurable algorithms.
/// </summary>
/// <remarks>
///     This service routes ROI calculation requests to appropriate calculator implementations
///     (Center or Rule of Thirds) and supports intelligent feature detection via face detection.
/// </remarks>
public sealed class RoiResolver(FaceDetector faceDetector, ILogger<RoiResolver> logger)
{
    private readonly ReadOnlyDictionary<RoiType, RoiCalculator> _calculators =
        new Dictionary<RoiType, RoiCalculator>
        {
            { RoiType.Center, new CenterRoi(faceDetector) },
            { RoiType.RuleOfThirds, new RuleOfThirdsRoi(faceDetector) }
        }.AsReadOnly();

    /// <summary>
    ///     Calculates the Region of Interest for the specified image asynchronously.
    /// </summary>
    /// <param name="image">The source image to calculate ROI for.</param>
    /// <param name="targetSize">The desired size of the ROI region.</param>
    /// <param name="option">Optional configuration for the selected ROI algorithm.</param>
    /// <returns>A task that returns the calculated ROI rectangle.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the ROI type is not supported.</exception>
    public async ValueTask<Rectangle> CalculateRoiAsync(MagickImage image, Size targetSize,
        RoiOption option)
    {
        logger.LogDebug("Calculating ROI using {Type} algorithm for image ({Width}x{Height}) targeting {TargetSize}", 
            option.Type, image.Width, image.Height, targetSize);

        try
        {
            var roi = await GetCalculator(option.Type).CalculateRoiAsync(image, targetSize, option).ConfigureAwait(false);
            logger.LogDebug("Calculated ROI: {ROI}", roi);
            return roi;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate ROI using {Type} algorithm", option.Type);
            throw;
        }
    }

    private RoiCalculator GetCalculator(RoiType key)
    {
        return _calculators.TryGetValue(key, out var calculator)
            ? calculator
            : throw new ArgumentOutOfRangeException(nameof(key), key, null);
    }
}