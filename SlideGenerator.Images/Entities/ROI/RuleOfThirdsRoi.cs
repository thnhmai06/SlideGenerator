using System.Drawing;
using System.Numerics;
using SlideGenerator.Images.Entities.Detectors;
using SlideGenerator.Images.Models;
using SlideGenerator.Images.Models.Options;
using Mat = OpenCvSharp.Mat;

namespace SlideGenerator.Images.Entities.ROI;

/// <summary>
///     Calculates rule-of-thirds ROI with face and eye landmark fallbacks.
/// </summary>
internal sealed class RuleOfThirdsRoi(FaceDetector faceDetector) : RoiCalculator
{
    private static readonly Vector2 RuleOfThirdsPivot = new(0.5f, 0.333f);

    public override async ValueTask<Rectangle> CalculateRoiAsync(
        Mat mat, Size targetSize, RoiType type,
        RoiOption? option = null)
    {
        if (type != RoiType.RuleOfThirds)
            throw new ArgumentException($"Invalid ROI type '{type}' for {nameof(RuleOfThirdsRoi)}.", nameof(type));
        
        var ruleOption = option as RuleOfThirdsOption;
        var pivot = ruleOption?.Pivot ?? RuleOfThirdsPivot;
        var sourceSize = new Size(mat.Width, mat.Height);
        
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        if (faces.Count <= 0) return Utilities.CalculateAnchoredRectangle(sourceSize, targetSize);

        var eyeCenter = faces.Centroid(face => face.EyesCenter);
        if (eyeCenter.HasValue)
            return Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, eyeCenter.Value, pivot);

        var faceCenter = faces.Centroid(face => face.FaceCenter);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(sourceSize, targetSize);
    }
}