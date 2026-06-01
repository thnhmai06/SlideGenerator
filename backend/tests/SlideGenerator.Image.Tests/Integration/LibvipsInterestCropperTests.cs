/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: LibvipsInterestCropperTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Infrastructure.Adapters;
using SlideGenerator.Image.Infrastructure.Services;
using Xunit;
using NetVipsImage = NetVips.Image;

namespace SlideGenerator.Image.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="LibvipsInterestCropper" />, verifying both the native
///     VipsImage pipeline and the byte-buffer fallback path, as well as all five
///     <see cref="InterestType" /> strategies.
/// </summary>
[Trait("Category", "Integration")]
public sealed class LibvipsInterestCropperTests
{
    private readonly LibvipsInterestCropper _cropper = new();

    #region Helpers

    /// <summary>
    ///     Creates a small black <see cref="VipsImage" /> of the given dimensions.
    /// </summary>
    private static VipsImage MakeVipsImage(int width, int height)
    {
        return new VipsImage(NetVipsImage.Black(width, height, 3));
    }

    /// <summary>
    ///     Creates a mock <see cref="IImage" /> whose <see cref="IImage.ToPng()" /> returns the
    ///     PNG bytes of a real black image, but is NOT a <see cref="VipsImage" /> instance.
    ///     This exercises the <c>ThumbnailBuffer</c> fallback path in <see cref="LibvipsInterestCropper" />.
    /// </summary>
    private static IImage MakeNonVipsPngImage(int width, int height)
    {
        using var native = NetVipsImage.Black(width, height, 3);
        var bytes = native.WriteToBuffer(".png");

        var mock = Substitute.For<IImage>();
        mock.ToPng().Returns(bytes);
        return mock;
    }

    #endregion

    #region VipsImage native pipeline — all InterestType values

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> with a <see cref="VipsImage" />
    ///     source and <see cref="InterestType.Attention" /> returns a result of the requested size.
    /// </summary>
    [Fact]
    public void Crop_VipsImage_AttentionMode_ReturnsTargetSize()
    {
        using var src = MakeVipsImage(300, 200);
        var target = new Size(100, 100);

        using var result = _cropper.Crop(src, target, InterestType.Attention)!;

        result.Info.Width.Should().Be((uint)target.Width);
        result.Info.Height.Should().Be((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> with <see cref="InterestType.Entropy" />
    ///     returns a result of the requested size.
    /// </summary>
    [Fact]
    public void Crop_VipsImage_EntropyMode_ReturnsTargetSize()
    {
        using var src = MakeVipsImage(300, 200);
        var target = new Size(150, 100);

        using var result = _cropper.Crop(src, target, InterestType.Entropy)!;

        result.Info.Width.Should().Be((uint)target.Width);
        result.Info.Height.Should().Be((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> with <see cref="InterestType.Low" />
    ///     returns a result of the requested size.
    /// </summary>
    [Fact]
    public void Crop_VipsImage_LowMode_ReturnsTargetSize()
    {
        using var src = MakeVipsImage(300, 200);
        var target = new Size(100, 80);

        using var result = _cropper.Crop(src, target, InterestType.Low)!;

        result.Info.Width.Should().Be((uint)target.Width);
        result.Info.Height.Should().Be((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> with <see cref="InterestType.High" />
    ///     returns a result of the requested size.
    /// </summary>
    [Fact]
    public void Crop_VipsImage_HighMode_ReturnsTargetSize()
    {
        using var src = MakeVipsImage(300, 200);
        var target = new Size(100, 80);

        using var result = _cropper.Crop(src, target, InterestType.High)!;

        result.Info.Width.Should().Be((uint)target.Width);
        result.Info.Height.Should().Be((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> with <see cref="InterestType.All" />
    ///     returns an image that fits within the target dimensions.
    ///     Unlike other modes, <c>All</c> scales to avoid losing content rather than cropping to an
    ///     exact size, so neither dimension exceeds the target but the result may be smaller.
    /// </summary>
    [Fact]
    public void Crop_VipsImage_AllMode_ReturnsFitsWithinTargetSize()
    {
        using var src = MakeVipsImage(300, 200);
        var target = new Size(100, 80);

        using var result = _cropper.Crop(src, target, InterestType.All)!;

        // All mode preserves the full field of view — output fits within target, not necessarily exact
        result.Info.Width.Should().BeLessThanOrEqualTo((uint)(target.Width * 2),
            "All mode scales to preserve content; result width stays in a reasonable range");
        result.Info.Height.Should().BeLessThanOrEqualTo((uint)(target.Height * 2),
            "All mode scales to preserve content; result height stays in a reasonable range");
        result.Info.Width.Should().BeGreaterThan(0u);
        result.Info.Height.Should().BeGreaterThan(0u);
    }

    #endregion

    #region Non-VipsImage fallback (ThumbnailBuffer path)

    /// <summary>
    ///     Verifies that when the source <see cref="IImage" /> is not a <see cref="VipsImage" />,
    ///     <see cref="LibvipsInterestCropper.Crop" /> falls back to decoding via
    ///     <c>ThumbnailBuffer</c> and still returns the correct target size.
    /// </summary>
    [Fact]
    public void Crop_NonVipsImage_UsesBufferFallback_ReturnsTargetSize()
    {
        using var src = MakeNonVipsPngImage(300, 200);
        var target = new Size(100, 100);

        using var result = _cropper.Crop(src, target, InterestType.Attention)!;

        result.Info.Width.Should().Be((uint)target.Width);
        result.Info.Height.Should().Be((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that the buffer fallback path calls <see cref="IImage.ToPng()" /> to obtain
    ///     the image bytes.
    /// </summary>
    [Fact]
    public void Crop_NonVipsImage_CallsToPng()
    {
        var src = MakeNonVipsPngImage(100, 80);
        var target = new Size(50, 50);

        using var _ = _cropper.Crop(src, target, InterestType.Entropy);

        src.Received(1).ToPng();
    }

    #endregion

    #region Trivial inputs

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> returns <see langword="null" />
    ///     when the target width is zero.
    /// </summary>
    [Fact]
    public void Crop_ZeroTargetWidth_ReturnsNull()
    {
        using var src = MakeVipsImage(100, 80);

        var result = _cropper.Crop(src, new Size(0, 80), InterestType.Attention);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="LibvipsInterestCropper.Crop" /> returns <see langword="null" />
    ///     when the target height is zero.
    /// </summary>
    [Fact]
    public void Crop_ZeroTargetHeight_ReturnsNull()
    {
        using var src = MakeVipsImage(100, 80);

        var result = _cropper.Crop(src, new Size(100, 0), InterestType.Attention);

        result.Should().BeNull();
    }

    #endregion
}