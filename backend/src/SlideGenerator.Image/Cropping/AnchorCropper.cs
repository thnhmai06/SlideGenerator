/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: AnchorCropper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using System.Numerics;

using SlideGenerator.Image.FaceDetection;
using SlideGenerator.Image.Loading;
namespace SlideGenerator.Image.Cropping;

/// <summary>
///     Specifies which point on the subject to use as the anchor for ROI calculation.
/// </summary>
public enum AnchorType : byte
{
    /// <summary>
    ///     Image center. Origin = image center; unit X = image width, unit Y = image height.
    ///     <c>(0, 0)</c> = center, <c>(-0.5, -0.5)</c> = top-left, <c>(0.5, 0.5)</c> = bottom-right.
    /// </summary>
    Image,

    /// <summary>
    ///     Face bounding-box center. Origin = FaceCenter; unit X = rect.Width, unit Y = rect.Height.
    /// </summary>
    Face,

    /// <summary>
    ///     Eye midpoint. Origin = EyesCenter; unit = <c>dist(leftEye, rightEye)</c> for both axes.
    ///     <c>(-0.5, 0)</c> = left eye, <c>(0.5, 0)</c> = right eye.
    /// </summary>
    Eyes,

    /// <summary>
    ///     Nose landmark. Origin = Nose tip; unit X = face rect.Width, unit Y = face rect.Height.
    /// </summary>
    Nose,

    /// <summary>
    ///     Mouth midpoint. Origin = MouthCenter; unit = <c>dist(leftMouth, rightMouth)</c> for both axes.
    ///     <c>(-0.5, 0)</c> = left corner, <c>(0.5, 0)</c> = right corner.
    /// </summary>
    Mouth
}

/// <summary>
///     Performs anchor-based image cropping, optionally using face detection to locate the anchor point.
/// </summary>
public interface IAnchorCropper
{
    /// <summary>
    ///     Crops <paramref name="image" /> to <paramref name="targetSize" /> using the anchor strategy
    ///     defined by <paramref name="option" />.
    /// </summary>
    /// <param name="image">Source image. Not modified.</param>
    /// <param name="targetSize">Desired output dimensions.</param>
    /// <param name="option">Anchor strategy and pivot.</param>
    /// <returns>
    ///     A new <see cref="IImage" />, or <see langword="null" /> if the anchor could not be resolved
    ///     (e.g., no face detected for a face-based anchor).
    /// </returns>
    ValueTask<IImage?> CropAsync(IImage image, Size targetSize, AnchorOption option);
}

/// <summary>
///     Implements <see cref="IAnchorCropper" /> using geometry helpers and
///     <see cref="IFaceDetector" /> for face-based anchor resolution.
///     Source images are never mutated; all operations return new instances.
/// </summary>
public sealed class AnchorCropper(IFaceDetector faceDetector) : IAnchorCropper
{
    /// <inheritdoc />
    public async ValueTask<IImage?> CropAsync(IImage image, Size targetSize, AnchorOption option)
    {
        if (targetSize.Width <= 0 || targetSize.Height <= 0) return null;

        var sourceSize = new Size((int)image.Info.Width, (int)image.Info.Height);

        if (option.Type == AnchorType.Image)
            return CropAndResize(image, sourceSize, targetSize,
                ResolveImageAnchor(sourceSize, option.Ratio), option.Pivot);

        var faces = await faceDetector.DetectAsync(image).ConfigureAwait(false);
        if (faces.Count == 0) return null;

        var point = TryResolveAnchorPoint(option, faces);
        return point.HasValue
            ? CropAndResize(image, sourceSize, targetSize, point.Value, option.Pivot)
            : null;
    }

    #region Geometry helpers

    /// <summary>
    ///     Calculates the ROI, crops the source image, and resizes it to match the target aspect ratio.
    /// </summary>
    private static IImage CropAndResize(IImage source, Size sourceSize, Size targetSize, Point anchor, Vector2 pivot)
    {
        var roi = Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, anchor, pivot);
        using var cropped = source.Crop(roi);
        var current = new Size((int)cropped.Info.Width, (int)cropped.Info.Height);
        return cropped.Resize(current.GetMaxAspectSize(targetSize));
    }

    /// <summary>
    ///     Calculates an anchor point relative to the image dimensions using a ratio.
    /// </summary>
    private static Point ResolveImageAnchor(Size sourceSize, Vector2 ratio)
    {
        var c = sourceSize.CenterPoint();
        return new Point(
            (int)MathF.Round(c.X + ratio.X * sourceSize.Width),
            (int)MathF.Round(c.Y + ratio.Y * sourceSize.Height));
    }

    /// <summary>
    ///     Dispatches anchor resolution based on the specified <see cref="AnchorType" />.
    /// </summary>
    private static Point? TryResolveAnchorPoint(AnchorOption option, IReadOnlyList<Face> faces)
    {
        return option.Type switch
        {
            AnchorType.Face => ResolveFaceAnchor(option.Ratio, faces),
            AnchorType.Eyes => ResolveEyesAnchor(option.Ratio, faces),
            AnchorType.Nose => ResolveNoseAnchor(option.Ratio, faces),
            AnchorType.Mouth => ResolveMouthAnchor(option.Ratio, faces),
            _ => null
        };
    }

    /// <summary>
    ///     Resolves the anchor point based in the center and average size of detected faces.
    /// </summary>
    private static Point? ResolveFaceAnchor(Vector2 ratio, IReadOnlyList<Face> faces)
    {
        var origin = faces.Centroid(f => f.FaceCenter);
        if (!origin.HasValue) return null;
        var avgW = faces.Average(f => f.Rect.Width);
        var avgH = faces.Average(f => f.Rect.Height);
        return new Point(
            (int)MathF.Round(origin.Value.X + ratio.X * (float)avgW),
            (int)MathF.Round(origin.Value.Y + ratio.Y * (float)avgH));
    }

    /// <summary>
    ///     Resolves the anchor point based in the center and average distance between detected eyes.
    /// </summary>
    private static Point? ResolveEyesAnchor(Vector2 ratio, IReadOnlyList<Face> faces)
    {
        var origin = faces.Centroid(f => f.EyesCenter);
        if (!origin.HasValue) return null;
        var dist = faces
            .Where(f => f is { RightEye: not null, LeftEye: not null })
            .Select(f => f.RightEye!.Value.Distance(f.LeftEye!.Value))
            .DefaultIfEmpty(0f).Average();
        return new Point(
            (int)MathF.Round(origin.Value.X + ratio.X * dist),
            (int)MathF.Round(origin.Value.Y + ratio.Y * dist));
    }

    /// <summary>
    ///     Resolves the anchor point based on the average position of detected noses.
    /// </summary>
    private static Point? ResolveNoseAnchor(Vector2 ratio, IReadOnlyList<Face> faces)
    {
        var origin = faces.Centroid(f => f.Nose);
        if (!origin.HasValue) return null;
        var avgW = faces.Average(f => f.Rect.Width);
        var avgH = faces.Average(f => f.Rect.Height);
        return new Point(
            (int)MathF.Round(origin.Value.X + ratio.X * (float)avgW),
            (int)MathF.Round(origin.Value.Y + ratio.Y * (float)avgH));
    }

    /// <summary>
    ///     Resolves the anchor point based in the center and average width of detected mouths.
    /// </summary>
    private static Point? ResolveMouthAnchor(Vector2 ratio, IReadOnlyList<Face> faces)
    {
        var origin = faces.Centroid(f => f.MouthCenter);
        if (!origin.HasValue) return null;
        var dist = faces
            .Where(f => f is { RightMouth: not null, LeftMouth: not null })
            .Select(f => f.RightMouth!.Value.Distance(f.LeftMouth!.Value))
            .DefaultIfEmpty(0f).Average();
        return new Point(
            (int)MathF.Round(origin.Value.X + ratio.X * dist),
            (int)MathF.Round(origin.Value.Y + ratio.Y * dist));
    }

    #endregion
}
