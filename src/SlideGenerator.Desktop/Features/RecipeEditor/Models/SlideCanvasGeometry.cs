/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: SlideCanvasGeometry.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Avalonia;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Models;

/// <summary>
///     Pure scaling math for the slide canvas overlay (P4.2) — maps a shape's bounds (in the original slide's
///     pixel space, from <c>SlideSummary.SlideSize</c>/<c>ShapeSummary.Bounds</c>) onto the on-screen rectangle
///     it must occupy over the preview image. Must exactly match the image control's uniform-contain
///     (letterboxed) scaling — the view pins <c>Stretch="Uniform"</c> explicitly so this math and the actual
///     rendering never drift apart.
/// </summary>
public static class SlideCanvasGeometry
{
    /// <summary>
    ///     Scales <paramref name="shapeBounds" /> from slide pixel space into canvas-relative coordinates,
    ///     assuming the slide preview is drawn into <paramref name="canvasSize" /> with uniform-contain
    ///     (aspect-preserving, letterboxed) scaling.
    /// </summary>
    public static Rect ScaleBounds(SizeF slideSize, global::Avalonia.Size canvasSize, RectangleF shapeBounds)
    {
        if (slideSize.Width <= 0 || slideSize.Height <= 0 || canvasSize.Width <= 0 || canvasSize.Height <= 0)
            return default;

        var scale = Math.Min(canvasSize.Width / slideSize.Width, canvasSize.Height / slideSize.Height);
        var renderedWidth = slideSize.Width * scale;
        var renderedHeight = slideSize.Height * scale;
        var offsetX = (canvasSize.Width - renderedWidth) / 2.0;
        var offsetY = (canvasSize.Height - renderedHeight) / 2.0;

        return new Rect(
            offsetX + shapeBounds.X * scale,
            offsetY + shapeBounds.Y * scale,
            shapeBounds.Width * scale,
            shapeBounds.Height * scale);
    }
}
