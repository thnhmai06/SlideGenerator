using System.Drawing;
using System.Numerics;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;
using Size = System.Drawing.Size;

namespace SlideGenerator.Domain.Images.Entities;

/// <summary>
///     Calculates rule-of-thirds ROI with face and eye landmark fallbacks.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:28:25 GMT+7
public sealed class RuleOfThirdsRoi(IFaceDetectorProvider faceDetectorProvider) : IRoiCalculator
{
    private static readonly Vector2 RuleOfThirdsPivot = new(1 / 2f, 1 / 3f);

    /// <inheritdoc />
    public async ValueTask<Rectangle> CalculateRoiAsync(
        IImage mat, Size targetSize, RoiType type,
        RoiOption? options = null)
    {
        if (type != RoiType.RuleOfThirds)
            throw new ArgumentException($"Invalid ROI type '{type}' for {nameof(RuleOfThirdsRoi)}.", nameof(type));
        var ruleOption = options as RuleOfThirdsOption;
        var pivot = ruleOption?.Pivot ?? RuleOfThirdsPivot;

        var faceDetector = await faceDetectorProvider.GetDetectorAsync().ConfigureAwait(false);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        if (faces.Count <= 0)
            return Utilities.CalculateAnchoredRectangle(mat.Size, targetSize); // fallback

        // Try to use eye center first
        var eyeCenter = faces.Centroid(face => face.EyesCenter);
        if (eyeCenter.HasValue)
            return Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, eyeCenter.Value, pivot);

        // Fall back to face center if eyes not available
        var faceCenter = faces.Centroid(face => face.FaceCenter);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, faceCenter.Value, pivot)
            // No faces detected, use image center
            : Utilities.CalculateAnchoredRectangle(mat.Size, targetSize);
    }
}