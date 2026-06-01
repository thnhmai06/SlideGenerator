/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: InfrastructureUtilitiesTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using NetVips;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Infrastructure;
using Xunit;
using OpenCvSize = OpenCvSharp.Size;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Utilities" /> in the Infrastructure namespace, verifying
///     the <see cref="InterestType" />-to-libvips and <see cref="Size" />-to-OpenCV mapping methods.
///     These tests exercise only enum/struct conversions and require no native I/O.
/// </summary>
public sealed class InfrastructureUtilitiesTests
{
    #region ToVips

    /// <summary>
    ///     Verifies that <see cref="InterestType.Entropy" /> maps to
    ///     <see cref="Enums.Interesting.Entropy" />.
    /// </summary>
    [Fact]
    public void ToVips_Entropy_ReturnsInterestingEntropy()
    {
        InterestType.Entropy.ToVips().Should().Be(Enums.Interesting.Entropy);
    }

    /// <summary>
    ///     Verifies that <see cref="InterestType.Attention" /> maps to
    ///     <see cref="Enums.Interesting.Attention" />.
    /// </summary>
    [Fact]
    public void ToVips_Attention_ReturnsInterestingAttention()
    {
        InterestType.Attention.ToVips().Should().Be(Enums.Interesting.Attention);
    }

    /// <summary>
    ///     Verifies that <see cref="InterestType.Low" /> maps to
    ///     <see cref="Enums.Interesting.Low" />.
    /// </summary>
    [Fact]
    public void ToVips_Low_ReturnsInterestingLow()
    {
        InterestType.Low.ToVips().Should().Be(Enums.Interesting.Low);
    }

    /// <summary>
    ///     Verifies that <see cref="InterestType.High" /> maps to
    ///     <see cref="Enums.Interesting.High" />.
    /// </summary>
    [Fact]
    public void ToVips_High_ReturnsInterestingHigh()
    {
        InterestType.High.ToVips().Should().Be(Enums.Interesting.High);
    }

    /// <summary>
    ///     Verifies that <see cref="InterestType.All" /> maps to
    ///     <see cref="Enums.Interesting.All" />.
    /// </summary>
    [Fact]
    public void ToVips_All_ReturnsInterestingAll()
    {
        InterestType.All.ToVips().Should().Be(Enums.Interesting.All);
    }

    /// <summary>
    ///     Verifies that an unrecognised <see cref="InterestType" /> value (cast from an integer
    ///     not defined in the enum) throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Fact]
    public void ToVips_InvalidMode_ThrowsArgumentOutOfRangeException()
    {
        var invalidMode = (InterestType)999;

        var act = () => invalidMode.ToVips();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region ToOpenCv

    /// <summary>
    ///     Verifies that a positive <see cref="Size" /> converts to an OpenCvSharp <see cref="OpenCvSize" />
    ///     with the same width and height values.
    /// </summary>
    [Theory]
    [InlineData(100, 200)]
    [InlineData(416, 416)]
    [InlineData(1, 1)]
    public void ToOpenCv_PositiveSize_ReturnsSameValues(int width, int height)
    {
        var result = new Size(width, height).ToOpenCv();

        result.Width.Should().Be(width);
        result.Height.Should().Be(height);
    }

    /// <summary>
    ///     Verifies that a zero <see cref="Size" /> converts to a zero <see cref="OpenCvSize" />.
    /// </summary>
    [Fact]
    public void ToOpenCv_ZeroSize_ReturnsZeroOpenCvSize()
    {
        var result = new Size(0, 0).ToOpenCv();

        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }

    #endregion
}