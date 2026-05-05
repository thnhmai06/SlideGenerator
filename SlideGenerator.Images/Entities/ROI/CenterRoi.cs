using System.Drawing;
using System.Numerics;
using ImageMagick;
using SlideGenerator.Images.Entities.Detectors;
using SlideGenerator.Images.Models;
using SlideGenerator.Images.Models.Options;

namespace SlideGenerator.Images.Entities.ROI;

/// <summary>
///     Calculates the center Region of Interest (ROI).
/// </summary>
internal sealed class CenterRoi(FaceDetector faceDetector) : RoiCalculator
{
    public override async ValueTask<Rectangle> CalculateRoiAsync(MagickImage image, Size targetSize, RoiOption option)
    {
        if (option.Type != RoiType.Center)
            throw new ArgumentException($"Invalid ROI type '{option.Type}' for {nameof(CenterRoi)}.",
                nameof(option.Type));

        var centerOption = option as CenterOption;
        var pivot = centerOption!.Pivot;
        var sourceSize = new Size((int)image.Width, (int)image.Height);

        return option is CenterOption { UseFaceAlignment: true }
            ? await CalculateRoiByFaceAsync(image, targetSize, pivot).ConfigureAwait(false)
            : Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, sourceSize.CenterPoint(), pivot);
    }

    private async ValueTask<Rectangle> CalculateRoiByFaceAsync(MagickImage image, Size targetSize, Vector2 pivot)
    {
        var sourceSize = new Size((int)image.Width, (int)image.Height);
        using var mat = image.ToMat();
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        var faceCenter = faces.Centroid(face => face.FaceCenter);

        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, sourceSize.CenterPoint(), pivot);
    }
}