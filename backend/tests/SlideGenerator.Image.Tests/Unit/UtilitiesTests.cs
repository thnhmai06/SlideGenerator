/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: UtilitiesTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using System.Numerics;
using FluentAssertions;
using SlideGenerator.Image.Application;
using Xunit;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Utilities" />, verifying all geometry helpers, conversions,
///     and arithmetic operations. All tests are pure and require no I/O, mocks, or external libs.
/// </summary>
public sealed class UtilitiesTests
{
    #region ToPoint

    /// <summary>
    ///     Verifies that positive half-values round away from zero (i.e. up).
    /// </summary>
    [Fact]
    public void ToPoint_PositiveHalfValues_RoundsAwayFromZero()
    {
        new Vector2(1.5f, 2.5f).ToPoint().Should().Be(new Point(2, 3));
    }

    /// <summary>
    ///     Verifies that negative half-values round away from zero (i.e. down toward negative infinity).
    /// </summary>
    [Fact]
    public void ToPoint_NegativeHalfValues_RoundsAwayFromZero()
    {
        new Vector2(-1.5f, -2.5f).ToPoint().Should().Be(new Point(-2, -3));
    }

    /// <summary>
    ///     Verifies that values below 0.5 are truncated toward zero.
    /// </summary>
    [Fact]
    public void ToPoint_ValuesBelowHalf_TruncatesTowardZero()
    {
        new Vector2(1.4f, 2.4f).ToPoint().Should().Be(new Point(1, 2));
    }

    /// <summary>
    ///     Verifies that a zero vector returns the origin point.
    /// </summary>
    [Fact]
    public void ToPoint_ZeroVector_ReturnsOrigin()
    {
        Vector2.Zero.ToPoint().Should().Be(new Point(0, 0));
    }

    #endregion

    #region Centroid

    /// <summary>
    ///     Verifies that an empty list produces a null centroid.
    /// </summary>
    [Fact]
    public void Centroid_EmptyList_ReturnsNull()
    {
        Array.Empty<Point>().Centroid(p => p).Should().BeNull();
    }

    /// <summary>
    ///     Verifies that when every selector returns null the centroid is null.
    /// </summary>
    [Fact]
    public void Centroid_AllSelectorsReturnNull_ReturnsNull()
    {
        new[] { 1, 2, 3 }.ToList().AsReadOnly().Centroid(_ => null).Should().BeNull();
    }

    /// <summary>
    ///     Verifies that a single non-null point is returned as-is.
    /// </summary>
    [Fact]
    public void Centroid_SinglePoint_ReturnsThatPoint()
    {
        var result = new[] { new Point(10, 20) }.ToList().AsReadOnly()
            .Centroid(p => p);

        result.Should().Be(new Point(10, 20));
    }

    /// <summary>
    ///     Verifies that the centroid of multiple points is their average, rounded away from zero.
    /// </summary>
    [Fact]
    public void Centroid_TwoPoints_ReturnsAverage()
    {
        var result = new[] { new Point(0, 0), new Point(10, 20) }.ToList().AsReadOnly()
            .Centroid(p => p);

        result.Should().Be(new Point(5, 10));
    }

    /// <summary>
    ///     Verifies that null entries are skipped and only non-null points contribute to the average.
    /// </summary>
    [Fact]
    public void Centroid_MixedNullAndNonNull_SkipsNulls()
    {
        var data = new Point?[] { new Point(10, 20), null, new Point(30, 40) }.ToList().AsReadOnly();
        var result = data.Centroid(p => p);

        // Average of (10,20) and (30,40) = (20,30)
        result.Should().Be(new Point(20, 30));
    }

    #endregion

    #region CalculateAnchoredRectangle

    /// <summary>
    ///     Verifies that default anchor (image center) and default pivot (0.5,0.5) produce
    ///     a rectangle centered in the source image.
    /// </summary>
    [Fact]
    public void CalculateAnchoredRectangle_DefaultAnchorAndPivot_CentersRoiInImage()
    {
        // 300×200 image, 100×100 crop
        // anchor = (150,100), pivot=(0.5,0.5): x=150-50=100, y=100-50=50
        var result = Utilities.CalculateAnchoredRectangle(new Size(300, 200), new Size(100, 100));

        result.Should().Be(new Rectangle(100, 50, 100, 100));
    }

    /// <summary>
    ///     Verifies that when crop size exceeds source dimensions, the output is clamped to source bounds.
    /// </summary>
    [Fact]
    public void CalculateAnchoredRectangle_CropLargerThanSource_ClampedToSourceBounds()
    {
        // 50×50 image, 200×200 crop → bounded to 50×50
        var result = Utilities.CalculateAnchoredRectangle(new Size(50, 50), new Size(200, 200));

        result.Width.Should().Be(50);
        result.Height.Should().Be(50);
        result.X.Should().BeGreaterThanOrEqualTo(0);
        result.Y.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an anchor near the top-left edge causes ClampIn to shift the rectangle
    ///     into the image rather than going negative.
    /// </summary>
    [Fact]
    public void CalculateAnchoredRectangle_AnchorNearTopLeft_ClampedIntoImage()
    {
        // anchor=(10,10), pivot=(0.5,0.5), crop=100×100: raw x=10-50=-40 → clamped to 0
        var result = Utilities.CalculateAnchoredRectangle(
            new Size(300, 200), new Size(100, 100),
            new Point(10, 10));

        result.X.Should().Be(0);
        result.Y.Should().Be(0);
        result.Width.Should().Be(100);
        result.Height.Should().Be(100);
    }

    /// <summary>
    ///     Verifies that pivot (0,0) places the top-left of the crop rectangle at the anchor point.
    /// </summary>
    [Fact]
    public void CalculateAnchoredRectangle_ZeroPivot_TopLeftAtAnchor()
    {
        // anchor=(50,40), pivot=(0,0): x=50, y=40 (no offset)
        var result = Utilities.CalculateAnchoredRectangle(
            new Size(300, 200), new Size(80, 60),
            new Point(50, 40),
            Vector2.Zero);

        result.X.Should().Be(50);
        result.Y.Should().Be(40);
    }

    /// <summary>
    ///     Verifies that pivot (1,1) places the bottom-right of the crop rectangle at the anchor point.
    /// </summary>
    [Fact]
    public void CalculateAnchoredRectangle_UnitPivot_BottomRightAtAnchor()
    {
        // anchor=(180,140), pivot=(1,1), crop=80×60: x=round(180-80)=100, y=round(140-60)=80
        var result = Utilities.CalculateAnchoredRectangle(
            new Size(300, 200), new Size(80, 60),
            new Point(180, 140),
            Vector2.One);

        result.X.Should().Be(100);
        result.Y.Should().Be(80);
        result.Width.Should().Be(80);
        result.Height.Should().Be(60);
    }

    /// <summary>
    ///     Documents current behaviour when crop size is zero: returns a zero-dimension rectangle
    ///     without throwing. This silently produces an invalid input for VipsImage.Crop. // BUG?
    /// </summary>
    [Fact]
    public void CalculateAnchoredRectangle_ZeroCropSize_ReturnsZeroSizeRectWithoutThrowing()
    {
        // No guard on zero crop — returns zero-sized rect; libvips will throw downstream // BUG?
        var act = () => Utilities.CalculateAnchoredRectangle(new Size(300, 200), new Size(0, 0));

        act.Should().NotThrow();
        act().Width.Should().Be(0);
        act().Height.Should().Be(0);
    }

    #endregion

    #region Point.ClampIn

    /// <summary>
    ///     Verifies that a point already inside the border is returned unchanged.
    /// </summary>
    [Fact]
    public void ClampIn_PointInsideBorder_ReturnsSamePoint()
    {
        new Point(50, 40).ClampIn(new Rectangle(0, 0, 200, 100)).Should().Be(new Point(50, 40));
    }

    /// <summary>
    ///     Verifies that a point to the left of the border is clamped to the left edge.
    /// </summary>
    [Fact]
    public void ClampIn_PointLeftOfBorder_ClampsToLeft()
    {
        new Point(-10, 40).ClampIn(new Rectangle(0, 0, 200, 100)).Should().Be(new Point(0, 40));
    }

    /// <summary>
    ///     Verifies that a point past the right edge is clamped to Right-1 (one pixel inside).
    /// </summary>
    [Fact]
    public void ClampIn_PointRightOfBorder_ClampsToRightMinusOne()
    {
        // border Right=200, so clamp max = 199
        new Point(210, 40).ClampIn(new Rectangle(0, 0, 200, 100)).Should().Be(new Point(199, 40));
    }

    /// <summary>
    ///     Verifies that a point past the bottom edge is clamped to Bottom-1 (one pixel inside).
    /// </summary>
    [Fact]
    public void ClampIn_PointBelowBorder_ClampsToBottomMinusOne()
    {
        // border Bottom=100, clamp max = 99
        new Point(50, 110).ClampIn(new Rectangle(0, 0, 200, 100)).Should().Be(new Point(50, 99));
    }

    /// <summary>
    ///     Verifies that clamping into a zero-width border returns the border's top-left corner
    ///     rather than throwing, since there is no valid pixel region inside.
    /// </summary>
    [Fact]
    public void ClampIn_ZeroWidthBorder_ReturnsBorderTopLeft()
    {
        var result = new Point(50, 50).ClampIn(new Rectangle(10, 20, 0, 100));

        result.Should().Be(new Point(10, 20));
    }

    #endregion

    #region Point.Distance

    /// <summary>
    ///     Verifies that the distance from a point to itself is zero.
    /// </summary>
    [Fact]
    public void Distance_SamePoint_ReturnsZero()
    {
        new Point(10, 20).Distance(new Point(10, 20)).Should().Be(0f);
    }

    /// <summary>
    ///     Verifies that the distance between two horizontally aligned points equals their X delta.
    /// </summary>
    [Fact]
    public void Distance_HorizontalPoints_ReturnsDeltaX()
    {
        new Point(0, 0).Distance(new Point(50, 0)).Should().BeApproximately(50f, 0.001f);
    }

    /// <summary>
    ///     Verifies the 3-4-5 right triangle: distance from (0,0) to (3,4) equals 5.
    /// </summary>
    [Fact]
    public void Distance_ThreeFourFiveTriangle_ReturnsFive()
    {
        new Point(0, 0).Distance(new Point(3, 4)).Should().BeApproximately(5f, 0.001f);
    }

    #endregion

    #region Point.ToVector2

    /// <summary>
    ///     Verifies that a positive point converts correctly to a Vector2.
    /// </summary>
    [Fact]
    public void ToVector2_PositivePoint_ReturnsCorrectVector()
    {
        new Point(30, 40).ToVector2().Should().Be(new Vector2(30, 40));
    }

    /// <summary>
    ///     Verifies that a negative point converts correctly to a Vector2.
    /// </summary>
    [Fact]
    public void ToVector2_NegativePoint_ReturnsCorrectVector()
    {
        new Point(-10, -20).ToVector2().Should().Be(new Vector2(-10, -20));
    }

    #endregion

    #region Rectangle.Lerp

    /// <summary>
    ///     Verifies that pivot (0,0) returns the top-left corner of the rectangle.
    /// </summary>
    [Fact]
    public void Lerp_ZeroPivot_ReturnsTopLeft()
    {
        new Rectangle(10, 20, 100, 80).Lerp(Vector2.Zero).Should().Be(new Point(10, 20));
    }

    /// <summary>
    ///     Verifies that pivot (0.5,0.5) returns the center of the rectangle.
    /// </summary>
    [Fact]
    public void Lerp_HalfPivot_ReturnsCenter()
    {
        // rect=(10,20,100,80): x+round(100*0.5)=60, y+round(80*0.5)=60
        new Rectangle(10, 20, 100, 80).Lerp(new Vector2(0.5f, 0.5f)).Should().Be(new Point(60, 60));
    }

    /// <summary>
    ///     Verifies that pivot (1,1) returns the bottom-right corner.
    /// </summary>
    [Fact]
    public void Lerp_UnitPivot_ReturnsBottomRight()
    {
        // rect=(10,20,100,80): x+100=110, y+80=100
        new Rectangle(10, 20, 100, 80).Lerp(Vector2.One).Should().Be(new Point(110, 100));
    }

    #endregion

    #region Rectangle.ClampIn

    /// <summary>
    ///     Verifies that a rectangle fully inside the border is returned unchanged.
    /// </summary>
    [Fact]
    public void RectClampIn_InsideBorder_ReturnsSameRect()
    {
        var rect = new Rectangle(10, 10, 50, 30);
        rect.ClampIn(new Rectangle(0, 0, 200, 100)).Should().Be(rect);
    }

    /// <summary>
    ///     Verifies that a rectangle wider than the border is resized to the border width and
    ///     repositioned to the border's left.
    /// </summary>
    [Fact]
    public void RectClampIn_RectWiderThanBorder_ClampsWidthAndSlidesToLeft()
    {
        var result = new Rectangle(0, 0, 300, 50).ClampIn(new Rectangle(0, 0, 200, 100));

        result.Width.Should().Be(200);
        result.X.Should().Be(0);
    }

    /// <summary>
    ///     Verifies that a rectangle taller than the border is resized to border height.
    /// </summary>
    [Fact]
    public void RectClampIn_RectTallerThanBorder_ClampsHeight()
    {
        var result = new Rectangle(0, 0, 50, 300).ClampIn(new Rectangle(0, 0, 200, 100));

        result.Height.Should().Be(100);
    }

    /// <summary>
    ///     Verifies that a rectangle extending past the right edge slides left to fit inside.
    /// </summary>
    [Fact]
    public void RectClampIn_RectOffRight_SlidesLeft()
    {
        // rect right = 190+50=240 > border right 200 → x = 200-50=150
        var result = new Rectangle(190, 10, 50, 30).ClampIn(new Rectangle(0, 0, 200, 100));

        result.X.Should().Be(150);
        result.Width.Should().Be(50);
    }

    /// <summary>
    ///     Verifies that a rectangle extending past the bottom slides up to fit inside.
    /// </summary>
    [Fact]
    public void RectClampIn_RectOffBottom_SlidesUp()
    {
        // rect bottom = 80+30=110 > border bottom 100 → y = 100-30=70
        var result = new Rectangle(10, 80, 50, 30).ClampIn(new Rectangle(0, 0, 200, 100));

        result.Y.Should().Be(70);
        result.Height.Should().Be(30);
    }

    /// <summary>
    ///     Verifies that a rectangle larger than the border in both axes is resized to border
    ///     dimensions and placed at the border origin.
    /// </summary>
    [Fact]
    public void RectClampIn_RectLargerInBothAxes_ResizedToBorderAndPlacedAtOrigin()
    {
        var result = new Rectangle(0, 0, 500, 400).ClampIn(new Rectangle(0, 0, 200, 100));

        result.Should().Be(new Rectangle(0, 0, 200, 100));
    }

    #endregion

    #region Size.GetMaxAspectSize

    /// <summary>
    ///     Verifies that when the original is wider than the ratio, height determines the output
    ///     and width is computed from the ratio.
    /// </summary>
    [Fact]
    public void GetMaxAspectSize_WiderOriginalThanRatio_ConstrainedByHeight()
    {
        // original=400×300 (4:3), ratio=100×100 (1:1)
        // originalAspect(1.333) >= ratioAspect(1.0) → height=300, width=round(300*1.0)=300
        new Size(400, 300).GetMaxAspectSize(new Size(100, 100)).Should().Be(new Size(300, 300));
    }

    /// <summary>
    ///     Verifies that when the original is taller than the ratio, width determines the output.
    /// </summary>
    [Fact]
    public void GetMaxAspectSize_TallerOriginalThanRatio_ConstrainedByWidth()
    {
        // original=300×400 (3:4), ratio=100×100 (1:1)
        // originalAspect(0.75) < ratioAspect(1.0) → width=300, height=round(300/1.0)=300
        new Size(300, 400).GetMaxAspectSize(new Size(100, 100)).Should().Be(new Size(300, 300));
    }

    /// <summary>
    ///     Verifies that the result never exceeds the original in either dimension.
    /// </summary>
    [Theory]
    [InlineData(100, 50, 3, 4)]
    [InlineData(200, 150, 16, 9)]
    [InlineData(64, 64, 1, 1)]
    public void GetMaxAspectSize_ResultNeverExceedsOriginal(
        int origW, int origH, int ratioW, int ratioH)
    {
        var result = new Size(origW, origH).GetMaxAspectSize(new Size(ratioW, ratioH));

        result.Width.Should().BeLessThanOrEqualTo(origW);
        result.Height.Should().BeLessThanOrEqualTo(origH);
    }

    /// <summary>
    ///     Documents current behaviour when original height is zero: returns Size(0,0) without
    ///     throwing, producing an unusable result. // BUG?
    /// </summary>
    [Fact]
    public void GetMaxAspectSize_ZeroOriginalHeight_ReturnsSilentlyWrongResultWithoutThrowing()
    {
        // original.Height=0 → Infinity/Infinity arithmetic → (0,0) // BUG?
        var act = () => new Size(100, 0).GetMaxAspectSize(new Size(16, 9));

        act.Should().NotThrow();
        act().Width.Should().Be(0);
        act().Height.Should().Be(0);
    }

    /// <summary>
    ///     Documents current behaviour when ratio height is zero: returns a width-only size
    ///     without throwing, silently discarding the height. // BUG?
    /// </summary>
    [Fact]
    public void GetMaxAspectSize_ZeroRatioHeight_ReturnsSilentlyWrongResultWithoutThrowing()
    {
        // ratioAspect = Infinity → height rounded to 0 // BUG?
        var act = () => new Size(100, 100).GetMaxAspectSize(new Size(16, 0));

        act.Should().NotThrow();
        act().Height.Should().Be(0);
    }

    #endregion

    #region Size.CenterPoint

    /// <summary>
    ///     Verifies that even dimensions produce an exact center point.
    /// </summary>
    [Fact]
    public void CenterPoint_EvenDimensions_ReturnsExactCenter()
    {
        new Size(200, 100).CenterPoint().Should().Be(new Point(100, 50));
    }

    /// <summary>
    ///     Verifies that odd dimensions produce a center point that truncates toward zero
    ///     (integer division, not rounding).
    /// </summary>
    [Fact]
    public void CenterPoint_OddDimensions_TruncatesDown()
    {
        new Size(301, 201).CenterPoint().Should().Be(new Point(150, 100));
    }

    #endregion

    #region Size.Lerp

    /// <summary>
    ///     Verifies that pivot (0,0) returns the origin.
    /// </summary>
    [Fact]
    public void SizeLerp_ZeroPivot_ReturnsOrigin()
    {
        new Size(300, 200).Lerp(Vector2.Zero).Should().Be(new Point(0, 0));
    }

    /// <summary>
    ///     Verifies that pivot (0.5,0.5) returns the half-size point, rounded away from zero.
    /// </summary>
    [Fact]
    public void SizeLerp_HalfPivot_ReturnsHalfSize()
    {
        new Size(300, 200).Lerp(new Vector2(0.5f, 0.5f)).Should().Be(new Point(150, 100));
    }

    /// <summary>
    ///     Verifies that pivot (1,1) returns the full size as a point.
    /// </summary>
    [Fact]
    public void SizeLerp_UnitPivot_ReturnsSizePoint()
    {
        new Size(300, 200).Lerp(Vector2.One).Should().Be(new Point(300, 200));
    }

    #endregion
}