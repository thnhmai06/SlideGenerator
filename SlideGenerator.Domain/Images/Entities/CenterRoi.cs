using System.Drawing;
using System.Numerics;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Domain.Images.Entities;

/// Reviewed by @thnhmai06 at 16/04/2026 13:45:59 GMT+7
public sealed class CenterRoi(IFaceDetectorProvider faceDetectorProvider) : IRoiCalculator
{
    private static readonly Vector2 CenterPivot = new(1 / 2f, 1 / 2f);

    /// <inheritdoc />
    public ValueTask<Rectangle> CalculateRoiAsync(
        IImage mat, Size targetSize, RoiType type,
        RoiOption? options = null)
    {
        if (type != RoiType.Center)
            throw new ArgumentException($"Invalid ROI type '{type}' for {nameof(CenterRoi)}.", nameof(type));

        var pivot = options?.Pivot ?? CenterPivot;
        var centerOption = options as CenterOption;

        return centerOption?.UseFaceAlignment != true
            ? ValueTask.FromResult(
                Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, mat.Size.CenterPoint(), pivot))
            : CalculateRoiByFaceAsync(mat, targetSize, pivot);
    }

    private async ValueTask<Rectangle> CalculateRoiByFaceAsync(IImage mat, Size targetSize, Vector2 pivot)
    {
        var faceCenter = await GetFaceCenterAsync(mat).ConfigureAwait(false);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, mat.Size.CenterPoint(), pivot);
    }

    private async ValueTask<Point?> GetFaceCenterAsync(IImage mat)
    {
        var faceDetector = await faceDetectorProvider.GetDetectorAsync().ConfigureAwait(false);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        return faces.Centroid(face => face.FaceCenter);
    }
}