/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: RoiResolverIntegrationTests.cs
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
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Tests.Integration.Fixtures;
using Xunit;

namespace SlideGenerator.Image.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="SlideGenerator.Image.Application.Services.RoiResolver" />
///     using real images and a real <see cref="SlideGenerator.Image.Infrastructure.Adapters.YuNet" />
///     face detector. Tests are skipped when the local image cache is empty.
/// </summary>
[Collection("FaceIntegration")]
[Trait("Category", "Integration")]
public sealed class RoiResolverIntegrationTests(FaceDatasetFixture dataset, ImageServiceFixture services)
{
    #region RuleOfThirdsOption

    /// <summary>
    ///     Verifies that <see cref="IRoiResolver.CalculateRoiAsync" /> with <see cref="RuleOfThirdsOption" />
    ///     produces an ROI that lies entirely within the source image bounds on a real CelebA-HQ portrait.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_RuleOfThirds_RoiWithinImageBounds()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageFactory.Open(path);

        var targetSize = new Size(512, 384);
        var option = new RuleOfThirdsOption();

        var roi = await services.RoiResolver.CalculateRoiAsync(image, targetSize, option);

        AssertRoiWithinBounds(roi, image.Info.Width, image.Info.Height, targetSize);
    }

    #endregion

    #region Multiple images — bounds invariant

    /// <summary>
    ///     Verifies the ROI bounds invariant across a sample of 5 random single-portrait images,
    ///     asserting that the ROI always lies within the source image regardless of face position.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_SampleOfSinglePortraits_RoiAlwaysWithinBounds()
    {
        var allImages = dataset.GetSingleImages();
        if (allImages.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var sample = allImages
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(5, allImages.Length))
            .ToArray();

        var targetSize = new Size(400, 400);
        var option = new CenterOption { UseFaceAlignment = true };

        foreach (var path in sample)
        {
            using var image = services.ImageFactory.Open(path);
            var roi = await services.RoiResolver.CalculateRoiAsync(image, targetSize, option);
            AssertRoiWithinBounds(roi, image.Info.Width, image.Info.Height, targetSize);
        }
    }

    #endregion

    #region Helpers

    private static void AssertRoiWithinBounds(Rectangle roi, uint imageWidth, uint imageHeight, Size targetSize)
    {
        roi.X.Should().BeGreaterThanOrEqualTo(0);
        roi.Y.Should().BeGreaterThanOrEqualTo(0);
        roi.Right.Should().BeLessThanOrEqualTo((int)imageWidth);
        roi.Bottom.Should().BeLessThanOrEqualTo((int)imageHeight);
        roi.Width.Should().Be(targetSize.Width);
        roi.Height.Should().Be(targetSize.Height);
    }

    #endregion

    #region CenterOption

    /// <summary>
    ///     Verifies that <see cref="IRoiResolver.CalculateRoiAsync" /> with <see cref="CenterOption" />
    ///     and <c>UseFaceAlignment = true</c> produces an ROI that lies entirely within the source
    ///     image bounds when run on a real CelebA-HQ portrait.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_CenterWithFaceAlignment_RoiWithinImageBounds()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageFactory.Open(path);

        var targetSize = new Size(400, 400);
        var option = new CenterOption { UseFaceAlignment = true };

        var roi = await services.RoiResolver.CalculateRoiAsync(image, targetSize, option);

        AssertRoiWithinBounds(roi, image.Info.Width, image.Info.Height, targetSize);
    }

    /// <summary>
    ///     Verifies that <see cref="IRoiResolver.CalculateRoiAsync" /> with <see cref="CenterOption" />
    ///     and <c>UseFaceAlignment = false</c> produces the correct centered ROI on a real image.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_CenterWithoutFaceAlignment_RoiWithinImageBounds()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageFactory.Open(path);

        var targetSize = new Size(300, 300);
        var option = new CenterOption { UseFaceAlignment = false };

        var roi = await services.RoiResolver.CalculateRoiAsync(image, targetSize, option);

        AssertRoiWithinBounds(roi, image.Info.Width, image.Info.Height, targetSize);
    }

    #endregion
}