/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: ImageUtilitiesTests.cs
 */

using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using FluentAssertions;
using SlideGenerator.Image;
using Xunit;

namespace SlideGenerator.Tests.Image;

public sealed class ImageUtilitiesTests
{
    [Fact]
    public void ClampIn_ShouldKeepPointWithinBounds()
    {
        var border = new Rectangle(0, 0, 100, 100);
        
        new Point(50, 50).ClampIn(border).Should().Be(new Point(50, 50));
        new Point(-10, 50).ClampIn(border).Should().Be(new Point(0, 50));
        new Point(50, 150).ClampIn(border).Should().Be(new Point(50, 99));
        new Point(100, 100).ClampIn(border).Should().Be(new Point(99, 99));
    }

    [Fact]
    public void CalculateAnchoredRectangle_ShouldCenterByDefault()
    {
        var sourceSize = new Size(100, 100);
        var cropSize = new Size(50, 50);

        var result = Utilities.CalculateAnchoredRectangle(sourceSize, cropSize);

        result.Should().Be(new Rectangle(25, 25, 50, 50));
    }

    [Fact]
    public void CalculateAnchoredRectangle_ShouldRespectPivot()
    {
        var sourceSize = new Size(100, 100);
        var cropSize = new Size(50, 50);
        var anchor = new Point(0, 0);
        var pivot = new Vector2(0, 0); // Top-left

        var result = Utilities.CalculateAnchoredRectangle(sourceSize, cropSize, anchor, pivot);

        result.Should().Be(new Rectangle(0, 0, 50, 50));
    }

    [Fact]
    public void Centroid_ShouldCalculateAveragePoint()
    {
        var points = new List<Point?>
        {
            new Point(0, 0),
            new Point(10, 10),
            new Point(20, 20)
        };

        var result = points.Centroid(p => p);

        result.Should().Be(new Point(10, 10));
    }

    [Fact]
    public void Centroid_ShouldReturnNull_WhenEmpty()
    {
        var points = new List<Point?>();
        points.Centroid(p => p).Should().BeNull();
    }

    [Fact]
    public void ToVector2_And_ToPoint_ShouldBeInverse()
    {
        var p = new Point(12, 34);
        p.ToVector2().ToPoint().Should().Be(p);
    }
}
