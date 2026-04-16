using System.Drawing;
using System.Numerics;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models.Roi;
using SlideGenerator.Domain.Images.Rules;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Images.Entities;

/// Reviewed by @thnhmai06 at 16/04/2026 13:45:59 GMT+7
public sealed class CenterRoi(IFaceDetectorProvider faceDetectorProvider) : RoiCalculator
{
    private static readonly Vector2 CenterPivot = new(1 / 2f, 1 / 2f);

    /// <inheritdoc />
    public override RoiType Type => RoiType.Center;

    /// <inheritdoc />
    public override ValueTask<Rectangle> CalculateRoiAsync(IMat mat, Size targetSize, RoiOption? options = null)
    {
        var pivot = options?.Pivot ?? CenterPivot;
        var centerOption = options as CenterOption;

        return centerOption?.UseFaceAlignment != true
            ? ValueTask.FromResult(
                Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, mat.Size.CenterPoint(), pivot))
            : CalculateRoiByFaceAsync(mat, targetSize, pivot);
    }

    private async ValueTask<Rectangle> CalculateRoiByFaceAsync(IMat mat, Size targetSize, Vector2 pivot)
    {
        var faceCenter = await GetFaceCenterAsync(mat).ConfigureAwait(false);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, mat.Size.CenterPoint(), pivot);
    }

    private async ValueTask<Point?> GetFaceCenterAsync(IMat mat)
    {
        var faceDetector = await faceDetectorProvider.GetCurrentDetectorAsync().ConfigureAwait(false);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        return faces.Centroid(face => face.FaceCenter);
    }
}