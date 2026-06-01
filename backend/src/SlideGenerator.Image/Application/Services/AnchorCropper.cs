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
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Domain.Models;

namespace SlideGenerator.Image.Application.Services;

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