using System.Drawing;
using System.Numerics;
using SlideGenerator.Images.Entities.Detectors;
using SlideGenerator.Images.Models;
using SlideGenerator.Images.Models.Options;
using Mat = OpenCvSharp.Mat;

namespace SlideGenerator.Images.Entities.ROI;

/// <summary>
///     Calculates the center Region of Interest (ROI).
/// </summary>
internal sealed class CenterRoi(FaceDetector faceDetector) : RoiCalculator
{
    private static readonly Vector2 CenterPivot = new(0.5f, 0.5f);

    public override async ValueTask<Rectangle> CalculateRoiAsync(
        Mat mat, Size targetSize, RoiType type,
        RoiOption? option = null)
    {
        if (type != RoiType.Center)
            throw new ArgumentException($"Invalid ROI type '{type}' for {nameof(CenterRoi)}.", nameof(type));

        var pivot = option?.Pivot ?? CenterPivot;
        var centerOption = option as CenterOption;
        var sourceSize = new Size(mat.Width, mat.Height);

        return centerOption?.UseFaceAlignment != true
            ? Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, sourceSize.CenterPoint(), pivot)
            : await CalculateRoiByFaceAsync(mat, targetSize, pivot).ConfigureAwait(false);
    }

    private async ValueTask<Rectangle> CalculateRoiByFaceAsync(Mat mat, Size targetSize, Vector2 pivot)
    {
        var sourceSize = new Size(mat.Width, mat.Height);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        var faceCenter = faces.Centroid(face => face.FaceCenter);
        
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, sourceSize.CenterPoint(), pivot);
    }
}