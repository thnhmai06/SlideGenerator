/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: AnchorCropperIntegrationTests.cs
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
///     Integration tests for <see cref="SlideGenerator.Image.Application.Services.AnchorCropper" />
///     using real images and a real <see cref="SlideGenerator.Image.Infrastructure.Adapters.YuNet" />
///     face detector. Tests are skipped when the local image cache is empty.
/// </summary>
[Collection("FaceIntegration")]
[Trait("Category", "Integration")]
public sealed class AnchorCropperIntegrationTests(FaceDatasetFixture dataset, ImageServiceFixture services)
{
    #region AnchorType.Eyes

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Eyes" /> with a rule-of-thirds pivot returns a
    ///     non-null image when eye landmarks are detected.
    /// </summary>
    [Fact]
    public async Task CropAsync_EyesAnchorRuleOfThirds_PortraitWithEyes_ReturnsNonNull()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);

        var faces = await services.FaceDetector.DetectAsync(image);
        var hasEyes = faces.Any(f => f is { RightEye: not null, LeftEye: not null });
        if (!hasEyes) Assert.Skip("No eye landmarks detected in this image — skip.");

        using var cropped = await services.AnchorCropper.CropAsync(image, new Size(512, 384),
            new AnchorOption { Type = AnchorType.Eyes, Pivot = new Vector2(0.5f, 1 / 3f) });

        cropped.Should().NotBeNull();
        cropped.Info.Width.Should().BeLessThanOrEqualTo(512u);
        cropped.Info.Height.Should().BeLessThanOrEqualTo(384u);
    }

    #endregion

    #region AnchorType.Image — always succeeds

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Image" /> returns a non-null image whose dimensions
    ///     fit within the requested target size (aspect-ratio preserving resize may produce smaller
    ///     dimensions on one axis).
    /// </summary>
    [Fact]
    public async Task CropAsync_ImageAnchor_ReturnsImageWithinTargetBounds()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        var target = new Size(300, 300);

        using var cropped = await services.AnchorCropper.CropAsync(image, target,
            new AnchorOption { Type = AnchorType.Image });

        cropped.Should().NotBeNull();
        cropped.Info.Width.Should().BeLessThanOrEqualTo((uint)target.Width);
        cropped.Info.Height.Should().BeLessThanOrEqualTo((uint)target.Height);
    }

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Image" /> with a non-zero ratio offsets the anchor
    ///     point and still returns a valid image.
    /// </summary>
    [Fact]
    public async Task CropAsync_ImageAnchorWithRatio_ReturnsNonNull()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);

        using var cropped = await services.AnchorCropper.CropAsync(image, new Size(200, 300),
            new AnchorOption { Type = AnchorType.Image, Ratio = new Vector2(0.1f, -0.1f) });

        cropped.Should().NotBeNull();
    }

    #endregion

    #region AnchorType.Face

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Face" /> returns a non-null image when at least one
    ///     face is detected in the portrait. The test is skipped (not failed) when YuNet detects
    ///     no face, as detection is probabilistic on the cached dataset.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceAnchor_PortraitWithFace_ReturnsNonNull()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);

        var faces = await services.FaceDetector.DetectAsync(image);
        if (faces.Count == 0) Assert.Skip("YuNet detected no face in this image — skip rather than fail.");

        using var cropped = await services.AnchorCropper.CropAsync(image, new Size(400, 400),
            new AnchorOption { Type = AnchorType.Face });

        cropped.Should().NotBeNull();
        cropped.Info.Width.Should().BeLessThanOrEqualTo(400u);
        cropped.Info.Height.Should().BeLessThanOrEqualTo(400u);
    }

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Face" /> returns <see langword="null" /> when
    ///     no face is detected.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceAnchor_GroupImageWithNoDetection_ReturnsNull()
    {
        var images = dataset.GetGroupImages();
        if (images.Length == 0) Assert.Skip("No group images cached.");

        // Pick an image where YuNet detects 0 faces (very small/occluded faces in crowd)
        string? zeroFacePath = null;
        foreach (var path in images.OrderBy(_ => Random.Shared.Next()).Take(10))
        {
            using var img = services.ImageLoader.Open(path);
            var faces = await services.FaceDetector.DetectAsync(img);
            if (faces.Count != 0) continue;
            zeroFacePath = path;
            break;
        }

        if (zeroFacePath is null) Assert.Skip("All sampled images had detectable faces — skip.");

        using var image = services.ImageLoader.Open(zeroFacePath);
        var result = await services.AnchorCropper.CropAsync(image, new Size(200, 200),
            new AnchorOption { Type = AnchorType.Face });

        result.Should().BeNull("AnchorCropper must return null when no face is detected");
    }

    #endregion
}