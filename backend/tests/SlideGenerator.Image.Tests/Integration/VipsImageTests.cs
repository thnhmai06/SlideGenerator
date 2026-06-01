/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: VipsImageTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using SlideGenerator.Image.Infrastructure.Adapters;
using Xunit;
using NetVipsImage = NetVips.Image;

namespace SlideGenerator.Image.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="VipsImage" />, exercising crop, resize, serialization,
///     and clone operations against real NetVips in-memory images.
///     No file I/O or network access is required; images are generated in memory via libvips.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VipsImageTests
{
    #region Helpers

    /// <summary>
    ///     Creates a small black RGB image of the given dimensions.
    /// </summary>
    private static VipsImage MakeBlack(int width, int height)
    {
        // srgb(0,0,0) — three bands so it behaves like a normal color image
        var native = NetVipsImage.Black(width, height, 3);
        return new VipsImage(native);
    }

    #endregion

    #region Info

    /// <summary>
    ///     Verifies that <see cref="VipsImage.Info" /> reports the correct width and height.
    /// </summary>
    [Fact]
    public void Info_CreatedImage_ReturnsCorrectDimensions()
    {
        using var img = MakeBlack(120, 80);

        img.Info.Width.Should().Be(120u);
        img.Info.Height.Should().Be(80u);
    }

    #endregion

    #region Resize

    /// <summary>
    ///     Verifies that <see cref="VipsImage.Resize" /> returns a new image with exactly the
    ///     requested dimensions (Force mode ignores aspect ratio).
    /// </summary>
    [Theory]
    [InlineData(200, 150)]
    [InlineData(50, 50)]
    [InlineData(1, 1)]
    public void Resize_ValidSize_ReturnsExactDimensions(int targetW, int targetH)
    {
        using var img = MakeBlack(100, 80);
        using var resized = img.Resize(new Size(targetW, targetH));

        resized.Info.Width.Should().Be((uint)targetW);
        resized.Info.Height.Should().Be((uint)targetH);
    }

    #endregion

    #region Crop

    /// <summary>
    ///     Verifies that <see cref="VipsImage.Crop" /> returns a new image with the exact
    ///     dimensions of the requested rectangle.
    /// </summary>
    [Fact]
    public void Crop_ValidRegion_ReturnsCorrectDimensions()
    {
        using var img = MakeBlack(200, 150);
        using var cropped = img.Crop(new Rectangle(10, 20, 80, 60));

        cropped.Info.Width.Should().Be(80u);
        cropped.Info.Height.Should().Be(60u);
    }

    /// <summary>
    ///     Verifies that <see cref="VipsImage.Crop" /> does not mutate the source image.
    /// </summary>
    [Fact]
    public void Crop_ValidRegion_SourceDimensionsUnchanged()
    {
        using var img = MakeBlack(200, 150);
        using var _ = img.Crop(new Rectangle(10, 20, 80, 60));

        img.Info.Width.Should().Be(200u);
        img.Info.Height.Should().Be(150u);
    }

    #endregion

    #region ToPng

    /// <summary>
    ///     Verifies that <see cref="VipsImage.ToPng()" /> returns a non-empty byte array that can
    ///     be decoded back as an image with the correct dimensions.
    /// </summary>
    [Fact]
    public void ToPng_Bytes_CanBeDecodedBack()
    {
        using var img = MakeBlack(60, 40);
        var bytes = img.ToPng();

        bytes.Should().NotBeEmpty();
        using var decoded = NetVipsImage.NewFromBuffer(bytes);
        decoded.Width.Should().Be(60);
        decoded.Height.Should().Be(40);
    }

    /// <summary>
    ///     Verifies that <see cref="VipsImage.ToPng(string)" /> writes a file that can be read
    ///     back with correct dimensions.
    /// </summary>
    [Fact]
    public void ToPng_FilePath_WritesReadablePngFile()
    {
        var path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        try
        {
            using var img = MakeBlack(60, 40);
            img.ToPng(path);

            File.Exists(path).Should().BeTrue();
            using var reloaded = NetVipsImage.NewFromFile(path);
            reloaded.Width.Should().Be(60);
            reloaded.Height.Should().Be(40);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    #endregion

    #region Clone

    /// <summary>
    ///     Verifies that a cloned <see cref="VipsImage" /> has the same dimensions as the original.
    /// </summary>
    [Fact]
    public void Clone_CreatedImage_HasSameDimensions()
    {
        using var img = MakeBlack(80, 60);
        using var clone = (VipsImage)((ICloneable)img).Clone();

        clone.Info.Width.Should().Be(img.Info.Width);
        clone.Info.Height.Should().Be(img.Info.Height);
    }

    /// <summary>
    ///     Verifies that the clone is a distinct object (not the same reference as the original).
    /// </summary>
    [Fact]
    public void Clone_CreatedImage_ReturnsNewInstance()
    {
        using var img = MakeBlack(80, 60);
        var clone = (VipsImage)((ICloneable)img).Clone();

        clone.Should().NotBeSameAs(img);
        clone.Dispose();
    }

    #endregion
}