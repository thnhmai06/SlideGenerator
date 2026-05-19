/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: FaceTests.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using System.Drawing;
using FluentAssertions;
using SlideGenerator.Image.Domain.Models;
using Xunit;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Face" />, verifying the computed properties
///     <see cref="Face.FaceCenter" />, <see cref="Face.EyesCenter" />, and <see cref="Face.MouthCenter" />.
///     All tests use pure values — no I/O or mocks.
/// </summary>
public sealed class FaceTests
{
    #region FaceCenter

    /// <summary>
    ///     Verifies that <see cref="Face.FaceCenter" /> returns the geometric center of the bounding
    ///     rectangle: (<c>X + Width/2</c>, <c>Y + Height/2</c>).
    /// </summary>
    [Fact]
    public void FaceCenter_StandardRect_ComputedFromRectCenter()
    {
        var face = new Face(new Rectangle(100, 80, 60, 40), 0.95f);

        face.FaceCenter.Should().Be(new Point(130, 100));
    }

    /// <summary>
    ///     Verifies that <see cref="Face.FaceCenter" /> returns the origin when the bounding
    ///     rectangle is positioned at (0, 0) with zero dimensions.
    /// </summary>
    [Fact]
    public void FaceCenter_ZeroRect_ReturnsOrigin()
    {
        var face = new Face(new Rectangle(0, 0, 0, 0), 1f);

        face.FaceCenter.Should().Be(new Point(0, 0));
    }

    #endregion

    #region EyesCenter

    /// <summary>
    ///     Verifies that <see cref="Face.EyesCenter" /> returns the average position of both eye
    ///     landmarks when both are present.
    /// </summary>
    [Fact]
    public void EyesCenter_BothEyesPresent_ReturnsMidpoint()
    {
        var face = new Face(
            new Rectangle(0, 0, 100, 100), 0.9f,
            new Point(30, 40),
            new Point(70, 40));

        face.EyesCenter.Should().Be(new Point(50, 40));
    }

    /// <summary>
    ///     Verifies that <see cref="Face.EyesCenter" /> returns <see langword="null" /> when the
    ///     right eye landmark is absent.
    /// </summary>
    [Fact]
    public void EyesCenter_RightEyeMissing_ReturnsNull()
    {
        var face = new Face(
            new Rectangle(0, 0, 100, 100), 0.9f,
            null,
            new Point(70, 40));

        face.EyesCenter.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="Face.EyesCenter" /> returns <see langword="null" /> when the
    ///     left eye landmark is absent.
    /// </summary>
    [Fact]
    public void EyesCenter_LeftEyeMissing_ReturnsNull()
    {
        var face = new Face(
            new Rectangle(0, 0, 100, 100), 0.9f,
            new Point(30, 40));

        face.EyesCenter.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="Face.EyesCenter" /> returns <see langword="null" /> when neither
    ///     eye landmark is provided.
    /// </summary>
    [Fact]
    public void EyesCenter_NeitherEyePresent_ReturnsNull()
    {
        var face = new Face(new Rectangle(0, 0, 100, 100), 0.9f);

        face.EyesCenter.Should().BeNull();
    }

    #endregion

    #region MouthCenter

    /// <summary>
    ///     Verifies that <see cref="Face.MouthCenter" /> returns the average position of both mouth
    ///     corner landmarks when both are present.
    /// </summary>
    [Fact]
    public void MouthCenter_BothCornersPresent_ReturnsMidpoint()
    {
        var face = new Face(
            new Rectangle(0, 0, 100, 100), 0.9f,
            RightMouth: new Point(40, 80),
            LeftMouth: new Point(60, 80));

        face.MouthCenter.Should().Be(new Point(50, 80));
    }

    /// <summary>
    ///     Verifies that <see cref="Face.MouthCenter" /> returns <see langword="null" /> when the
    ///     right mouth corner landmark is absent.
    /// </summary>
    [Fact]
    public void MouthCenter_RightMouthMissing_ReturnsNull()
    {
        var face = new Face(
            new Rectangle(0, 0, 100, 100), 0.9f,
            RightMouth: null,
            LeftMouth: new Point(60, 80));

        face.MouthCenter.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="Face.MouthCenter" /> returns <see langword="null" /> when neither
    ///     mouth corner landmark is provided.
    /// </summary>
    [Fact]
    public void MouthCenter_NeitherCornerPresent_ReturnsNull()
    {
        var face = new Face(new Rectangle(0, 0, 100, 100), 0.9f);

        face.MouthCenter.Should().BeNull();
    }

    #endregion
}