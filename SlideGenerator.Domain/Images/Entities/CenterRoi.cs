using System.Drawing;
using System.Numerics;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Domain.Images.Entities;

/// <summary>
///     Calculates the center Region of Interest (ROI).
/// </summary>
/// <param name="faceDetectorProvider">The provider used to resolve face detector instances.</param>
/// Reviewed by @thnhmai06 at 16/04/2026 13:45:59 GMT+7
public sealed class CenterRoi(IFaceDetectorProvider faceDetectorProvider) : IRoiCalculator
{
    /// <summary>
    ///     The default pivot point for the center ROI.
    /// </summary>
    private static readonly Vector2 CenterPivot = new(1 / 2f, 1 / 2f);

    /// <summary>
    ///     Calculates the ROI for the specified image and target size.
    /// </summary>
    /// <param name="mat">The source <see cref="IImage" />.</param>
    /// <param name="targetSize">The desired <see cref="Size" /> of the ROI.</param>
    /// <param name="type">The <see cref="RoiType" />.</param>
    /// <param name="options">Optional <see cref="RoiOption" />.</param>
    /// <returns>A <see cref="Rectangle" /> representing the calculated ROI.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type" /> is not <see cref="RoiType.Center" />.</exception>
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

    /// <summary>
    ///     Calculates the ROI aligned with detected faces.
    /// </summary>
    /// <param name="mat">The source <see cref="IImage" />.</param>
    /// <param name="targetSize">The desired <see cref="Size" /> of the ROI.</param>
    /// <param name="pivot">The <see cref="Vector2" /> pivot.</param>
    /// <returns>A <see cref="Rectangle" /> representing the face-aligned ROI.</returns>
    private async ValueTask<Rectangle> CalculateRoiByFaceAsync(IImage mat, Size targetSize, Vector2 pivot)
    {
        var faceCenter = await GetFaceCenterAsync(mat).ConfigureAwait(false);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(mat.Size, targetSize, mat.Size.CenterPoint(), pivot);
    }

    /// <summary>
    ///     Gets the centroid of all detected faces in the image.
    /// </summary>
    /// <param name="mat">The source <see cref="IImage" />.</param>
    /// <returns>The centroid <see cref="Point" />, or <see langword="null" /> if no faces are detected.</returns>
    private async ValueTask<Point?> GetFaceCenterAsync(IImage mat)
    {
        var faceDetector = await faceDetectorProvider.GetDetectorAsync().ConfigureAwait(false);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        return faces.Centroid(face => face.FaceCenter);
    }
}