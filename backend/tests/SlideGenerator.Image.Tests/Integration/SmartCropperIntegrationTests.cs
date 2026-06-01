/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: SmartCropperIntegrationTests.cs
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
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Tests.Fixtures;
using Xunit;

namespace SlideGenerator.Image.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="SlideGenerator.Image.Application.Services.SmartCropper" />
///     using real images and fully wired croppers.
///     Tests verify the fallback chain semantics end-to-end.
///     Tests are skipped when the local image cache is empty.
/// </summary>
[Collection("FaceIntegration")]
[Trait("Category", "Integration")]
public sealed class SmartCropperIntegrationTests(FaceDatasetFixture dataset, ImageServiceFixture services)
{
    #region Fallback chain — always returns non-null

    /// <summary>
    ///     Verifies that a <see cref="AnchorType.Face" /> → <see cref="AnchorType.Image" /> chain
    ///     always returns a non-null result because the <see cref="AnchorType.Image" /> fallback
    ///     never fails.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceThenImageChain_AlwaysReturnsNonNull()
    {
        var allImages = dataset.GetSingleImages();
        if (allImages.Length == 0) Assert.Skip("No single images cached.");

        var sample = allImages
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(5, allImages.Length))
            .ToArray();

        foreach (var path in sample)
        {
            using var image = services.ImageLoader.Open(path);
            using var cropped = await services.Cropper.CropAsync(image, new Size(400, 400),
                new AnchorOption { Type = AnchorType.Face },
                new AnchorOption { Type = AnchorType.Image });

            cropped.Should().NotBeNull(
                "Image fallback must always succeed even when no face is detected");
        }
    }

    /// <summary>
    ///     Verifies that a full rule-of-thirds chain
    ///     (<see cref="AnchorType.Eyes" /> → <see cref="AnchorType.Face" /> → <see cref="AnchorType.Image" />)
    ///     always returns a non-null result.
    /// </summary>
    [Fact]
    public async Task CropAsync_EyesFaceImageFallbackChain_AlwaysReturnsNonNull()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);

        using var cropped = await services.Cropper.CropAsync(image, new Size(512, 384),
            new AnchorOption { Type = AnchorType.Eyes, Pivot = new Vector2(0.5f, 1 / 3f) },
            new AnchorOption { Type = AnchorType.Face, Pivot = new Vector2(0.5f, 1 / 3f) },
            new AnchorOption { Type = AnchorType.Image });

        cropped.Should().NotBeNull();
    }

    #endregion

    #region InterestOption

    /// <summary>
    ///     Verifies that <see cref="InterestOption" /> returns an image with exactly the requested
    ///     target dimensions (libvips thumbnail crops and scales to exact size).
    /// </summary>
    [Fact]
    public async Task CropAsync_InterestOption_ReturnsExactTargetSize()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        var target = new Size(200, 200);

        using var cropped = await services.Cropper.CropAsync(image, target,
            new InterestOption { Type = InterestType.Attention });

        cropped.Should().NotBeNull();
        cropped.Info.Width.Should().Be((uint)target.Width);
        cropped.Info.Height.Should().Be((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that a chain of anchor options ending in <see cref="InterestOption" /> falls
    ///     through to the interest cropper when all anchor options fail.
    /// </summary>
    [Fact]
    public async Task CropAsync_AnchorFailsThenInterest_ReturnsExactTargetSize()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        var target = new Size(300, 200);

        using var cropped = await services.Cropper.CropAsync(image, target,
            new AnchorOption { Type = AnchorType.Eyes },
            new AnchorOption { Type = AnchorType.Face },
            new InterestOption { Type = InterestType.Entropy });

        cropped.Should().NotBeNull();
        cropped.Info.Width.Should().Be((uint)target.Width);
        cropped.Info.Height.Should().Be((uint)target.Height);
    }

    #endregion
}