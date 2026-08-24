/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: SlideCanvasGeometryTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Avalonia;
using FluentAssertions;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="SlideCanvasGeometry.ScaleBounds" />. The discriminating case is a slide whose
///     aspect ratio differs from the canvas — an aspect-matched test would pass even with the letterbox offset
///     math wrong, so every test here uses a mismatched 16:9 slide against a square canvas.
/// </summary>
public sealed class SlideCanvasGeometryTests
{
    private static readonly SizeF SlideSize = new(1920, 1080); // 16:9
    private static readonly Avalonia.Size CanvasSize = new(960, 960); // square — width-limited, letterboxed top/bottom

    /// <summary>
    ///     A shape at the slide's bottom-right corner must land on the rendered image's bottom-right corner
    ///     (960, 750 — the letterboxed image, not the canvas's own bottom edge at 960), proving the offset
    ///     axis and magnitude are both correct, not just the scale factor.
    /// </summary>
    [Fact]
    public void ScaleBounds_ShapeAtSlideBottomRightCorner_MapsToRenderedImageBottomRightCorner()
    {
        var shapeBounds = new RectangleF(1728, 972, 192, 108); // bottom-right 10% x 10% of the slide

        var result = SlideCanvasGeometry.ScaleBounds(SlideSize, CanvasSize, shapeBounds);

        (result.X + result.Width).Should().BeApproximately(960, 0.01); // image's right edge = canvas's right edge
        (result.Y + result.Height).Should().BeApproximately(750, 0.01); // image's bottom edge, NOT canvas's (960)
    }

    /// <summary>A shape at the slide's origin must land at the letterbox offset, not (0, 0).</summary>
    [Fact]
    public void ScaleBounds_ShapeAtSlideOrigin_MapsToLetterboxOffsetNotCanvasOrigin()
    {
        var result = SlideCanvasGeometry.ScaleBounds(SlideSize, CanvasSize, new RectangleF(0, 0, 100, 100));

        result.X.Should().BeApproximately(0, 0.01); // width-limited: no horizontal letterbox
        result.Y.Should().BeApproximately(210, 0.01); // (960 - 540) / 2 vertical letterbox
    }

    /// <summary>A degenerate (zero-size) slide or canvas must not throw or divide by zero.</summary>
    [Fact]
    public void ScaleBounds_ZeroSizeSlide_ReturnsDefaultRect()
    {
        var result = SlideCanvasGeometry.ScaleBounds(new SizeF(0, 0), CanvasSize, new RectangleF(0, 0, 10, 10));

        result.Should().Be(default(Rect));
    }
}
