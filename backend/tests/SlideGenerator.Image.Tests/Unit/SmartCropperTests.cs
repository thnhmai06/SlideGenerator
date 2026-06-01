/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: SmartCropperTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Application.Services;
using SlideGenerator.Image.Domain.Entities;
using Xunit;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SmartCropper" />, verifying that it walks the
///     <see cref="RoiOption" /> fallback chain and delegates each option to the correct cropper.
/// </summary>
public sealed class SmartCropperTests
{
    private readonly IAnchorCropper _anchorCropper = Substitute.For<IAnchorCropper>();
    private readonly SmartCropper _cropper;
    private readonly IInterestCropper _interestCropper = Substitute.For<IInterestCropper>();

    public SmartCropperTests()
    {
        _cropper = new SmartCropper(_anchorCropper, _interestCropper, NullLogger<SmartCropper>.Instance);
    }

    private static IImage MockImage(uint w = 300, uint h = 200)
    {
        var img = Substitute.For<IImage>();
        img.Info.Width.Returns(w);
        img.Info.Height.Returns(h);
        return img;
    }

    #region Empty options

    /// <summary>
    ///     Verifies that an empty option list returns <see langword="null" /> without calling any cropper.
    /// </summary>
    [Fact]
    public async Task CropAsync_NoOptions_ReturnsNull()
    {
        var result = await _cropper.CropAsync(MockImage(), new Size(100, 100));

        result.Should().BeNull();
        await _anchorCropper.DidNotReceive().CropAsync(Arg.Any<IImage>(), Arg.Any<Size>(), Arg.Any<AnchorOption>());
        _interestCropper.DidNotReceive().Crop(Arg.Any<IImage>(), Arg.Any<Size>(), Arg.Any<InterestType>());
    }

    #endregion

    #region Trivial inputs

    /// <summary>
    ///     Verifies that a zero target width returns <see langword="null" /> without calling any cropper.
    /// </summary>
    [Fact]
    public async Task CropAsync_ZeroTargetWidth_ReturnsNull()
    {
        var result = await _cropper.CropAsync(MockImage(), new Size(0, 100),
            new AnchorOption { Type = AnchorType.Image });

        result.Should().BeNull();
        await _anchorCropper.DidNotReceive().CropAsync(Arg.Any<IImage>(), Arg.Any<Size>(), Arg.Any<AnchorOption>());
    }

    /// <summary>
    ///     Verifies that a zero target height returns <see langword="null" /> without calling any cropper.
    /// </summary>
    [Fact]
    public async Task CropAsync_ZeroTargetHeight_ReturnsNull()
    {
        var result = await _cropper.CropAsync(MockImage(), new Size(100, 0),
            new InterestOption { Type = InterestType.Attention });

        result.Should().BeNull();
        _interestCropper.DidNotReceive().Crop(Arg.Any<IImage>(), Arg.Any<Size>(), Arg.Any<InterestType>());
    }

    #endregion

    #region AnchorOption delegation

    /// <summary>
    ///     Verifies that <see cref="SmartCropper" /> delegates <see cref="AnchorOption" /> to
    ///     <see cref="IAnchorCropper" /> and returns its result.
    /// </summary>
    [Fact]
    public async Task CropAsync_AnchorOption_DelegatesToAnchorCropper()
    {
        var source = MockImage();
        var expected = Substitute.For<IImage>();
        var target = new Size(100, 100);
        var option = new AnchorOption { Type = AnchorType.Image };
        _anchorCropper.CropAsync(source, target, option).Returns(new ValueTask<IImage?>(expected));

        var result = await _cropper.CropAsync(source, target, option);

        result.Should().BeSameAs(expected);
        await _anchorCropper.Received(1).CropAsync(source, target, option);
    }

    /// <summary>
    ///     Verifies that when the first <see cref="AnchorOption" /> returns <see langword="null" />
    ///     (e.g., no face), the next option in the chain is tried.
    /// </summary>
    [Fact]
    public async Task CropAsync_FirstAnchorFails_TriesNextOption()
    {
        var source = MockImage();
        var expected = Substitute.For<IImage>();
        var target = new Size(100, 100);
        var failOpt = new AnchorOption { Type = AnchorType.Face };
        var fallbackOpt = new AnchorOption { Type = AnchorType.Image };

        _anchorCropper.CropAsync(source, target, failOpt).Returns(new ValueTask<IImage?>((IImage?)null));
        _anchorCropper.CropAsync(source, target, fallbackOpt).Returns(new ValueTask<IImage?>(expected));

        var result = await _cropper.CropAsync(source, target, failOpt, fallbackOpt);

        result.Should().BeSameAs(expected);
        await _anchorCropper.Received(1).CropAsync(source, target, failOpt);
        await _anchorCropper.Received(1).CropAsync(source, target, fallbackOpt);
    }

    /// <summary>
    ///     Verifies that when all options fail, <see langword="null" /> is returned.
    /// </summary>
    [Fact]
    public async Task CropAsync_AllAnchorsFail_ReturnsNull()
    {
        var source = MockImage();
        var target = new Size(100, 100);
        _anchorCropper.CropAsync(Arg.Any<IImage>(), Arg.Any<Size>(), Arg.Any<AnchorOption>())
            .Returns(new ValueTask<IImage?>((IImage?)null));

        var result = await _cropper.CropAsync(source, target,
            new AnchorOption { Type = AnchorType.Face },
            new AnchorOption { Type = AnchorType.Eyes });

        result.Should().BeNull();
    }

    #endregion

    #region InterestOption delegation

    /// <summary>
    ///     Verifies that <see cref="SmartCropper" /> delegates <see cref="InterestOption" /> to
    ///     <see cref="IInterestCropper" /> and returns its result immediately.
    /// </summary>
    [Fact]
    public async Task CropAsync_InterestOption_DelegatesToInterestCropper()
    {
        var source = MockImage();
        var expected = Substitute.For<IImage>();
        var target = new Size(100, 100);
        _interestCropper.Crop(source, target, InterestType.Entropy).Returns(expected);

        var result = await _cropper.CropAsync(source, target,
            new InterestOption { Type = InterestType.Entropy });

        result.Should().BeSameAs(expected);
        _interestCropper.Received(1).Crop(source, target, InterestType.Entropy);
    }

    /// <summary>
    ///     Verifies that when an <see cref="AnchorOption" /> fails, the chain falls through to
    ///     <see cref="InterestOption" /> and <see cref="IInterestCropper" /> is called.
    /// </summary>
    [Fact]
    public async Task CropAsync_AnchorFailsThenInterest_InterestCropperCalled()
    {
        var source = MockImage();
        var expected = Substitute.For<IImage>();
        var target = new Size(100, 100);
        _anchorCropper.CropAsync(Arg.Any<IImage>(), Arg.Any<Size>(), Arg.Any<AnchorOption>())
            .Returns(new ValueTask<IImage?>((IImage?)null));
        _interestCropper.Crop(source, target, InterestType.Attention).Returns(expected);

        var result = await _cropper.CropAsync(source, target,
            new AnchorOption { Type = AnchorType.Eyes },
            new InterestOption { Type = InterestType.Attention });

        result.Should().BeSameAs(expected);
        _interestCropper.Received(1).Crop(source, target, InterestType.Attention);
    }

    #endregion
}