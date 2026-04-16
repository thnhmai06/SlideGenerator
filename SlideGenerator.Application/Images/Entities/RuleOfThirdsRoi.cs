using System.Drawing;
using System.Numerics;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models.Roi;
using SlideGenerator.Domain.Images.Rules;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Images.Entities;

/// <summary>
///     Calculates rule-of-thirds ROI with face and eye landmark fallbacks.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:28:25 GMT+7
public sealed class RuleOfThirdsRoi(IFaceDetectorProvider faceDetectorProvider) : RoiCalculator
{
    private static readonly Vector2 RuleOfThirdsPivot = new(1 / 2f, 1 / 3f);

    /// <inheritdoc />
    public override RoiType Type => RoiType.RuleOfThirds;

    /// <inheritdoc />
    public override async ValueTask<Rectangle> CalculateRoiAsync(IMat mat, Size targetSize, RoiOption? options = null)
    {
        var ruleOption = options as RuleOfThirdsOption;
        var relativePin = ruleOption?.Pivot ?? RuleOfThirdsPivot;

        var faceDetector = await faceDetectorProvider.GetCurrentDetectorAsync().ConfigureAwait(false);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        if (faces.Count <= 0)
            return Utilities.CalculateAnchoredRectangle(mat.Size, targetSize); // fallback

        // Try to use eye center first
        var eyeCenter = faces.Centroid(face => face.EyesCenter);
        if (eyeCenter.HasValue)
            return Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, eyeCenter.Value, relativePin);

        // Fall back to face center if eyes not available
        var faceCenter = faces.Centroid(face => face.FaceCenter);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, faceCenter.Value, relativePin)
            // No faces detected, use image center
            : Utilities.CalculateAnchoredRectangle(mat.Size, targetSize);
    }
}