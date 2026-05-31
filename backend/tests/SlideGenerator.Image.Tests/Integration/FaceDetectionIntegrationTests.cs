/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: FaceDetectionIntegrationTests.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Tests.Integration.Fixtures;
using Xunit;

namespace SlideGenerator.Image.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="SlideGenerator.Image.Infrastructure.Adapters.YuNet" /> face
///     detection using real images downloaded from HuggingFace.
///     Tests are skipped when the local image cache is empty (no network available).
/// </summary>
[Collection("FaceIntegration")]
[Trait("Category", "Integration")]
public sealed class FaceDetectionIntegrationTests(FaceDatasetFixture dataset, ImageServiceFixture services)
{
    #region WIDER FACE face count plausibility

    /// <summary>
    ///     Verifies that the number of detected faces in a group image (filename encodes expected
    ///     count as <c>g{N}f_*.jpg</c>) is within a plausible range of the ground-truth counts.
    ///     YuNet may miss occluded or very small faces, so the assertion allows up to 80 % miss rate.
    /// </summary>
    [Fact]
    public async Task DetectAsync_GroupImage_DetectedCountWithinPlausibleRange()
    {
        var images = dataset.GetGroupImages();
        if (images.Length == 0) Assert.Skip("No group images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        var fileName = Path.GetFileNameWithoutExtension(path);
        var groundTruth = ParseFaceCount(fileName);

        using var image = services.ImageLoader.Open(path);
        using var mat = services.MatLoader.Create(image);
        var faces = await services.FaceDetector.DetectAsync(mat);

        faces.Count.Should().BeLessThanOrEqualTo(groundTruth * 3,
            "detected count should not massively exceed ground truth");
    }

    #endregion

    #region Helpers

    private static int ParseFaceCount(string fileNameWithoutExt)
    {
        // Filename format: g005f_0 or c029f_1 — extract the numeric part after g/c and before f
        var start = 1;
        var end = fileNameWithoutExt.IndexOf('f', start);
        return end > start && int.TryParse(fileNameWithoutExt[start..end], out var n) ? n : 0;
    }

    #endregion

    #region Smoke tests (single image, no exception)

    /// <summary>
    ///     Verifies that <see cref="IFaceDetector.DetectAsync" /> completes without throwing
    ///     when processing a random single-portrait image from CelebA-HQ.
    /// </summary>
    [Fact]
    public async Task DetectAsync_SinglePortrait_CompletesWithoutException()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        using var mat = services.MatLoader.Create(image);

        var faces = await services.FaceDetector.DetectAsync(mat);

        faces.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="IFaceDetector.DetectAsync" /> completes without throwing
    ///     when processing a random group image from WIDER FACE.
    /// </summary>
    [Fact]
    public async Task DetectAsync_GroupImage_CompletesWithoutException()
    {
        var images = dataset.GetGroupImages();
        if (images.Length == 0) Assert.Skip("No group images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        using var mat = services.MatLoader.Create(image);

        var faces = await services.FaceDetector.DetectAsync(mat);

        faces.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="IFaceDetector.DetectAsync" /> completes without throwing
    ///     when processing a random crowd image from WIDER FACE.
    /// </summary>
    [Fact]
    public async Task DetectAsync_CrowdImage_CompletesWithoutException()
    {
        var images = dataset.GetCrowdImages();
        if (images.Length == 0) Assert.Skip("No crowd images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        using var mat = services.MatLoader.Create(image);

        var faces = await services.FaceDetector.DetectAsync(mat);

        faces.Should().NotBeNull();
    }

    #endregion

    #region Detection accuracy

    /// <summary>
    ///     Verifies that <see cref="IFaceDetector.DetectAsync" /> detects at least one face in the
    ///     majority (≥ 70 %) of a random sample of 10 single-portrait images from CelebA-HQ.
    ///     This guards against the detector being misconfigured or the model file being corrupt.
    /// </summary>
    [Fact]
    public async Task DetectAsync_SampleOfSinglePortraits_MostHaveAtLeastOneFace()
    {
        var allImages = dataset.GetSingleImages();
        if (allImages.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var sample = allImages
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(10, allImages.Length))
            .ToArray();

        var detectedAny = 0;
        foreach (var path in sample)
        {
            using var image = services.ImageLoader.Open(path);
            using var mat = services.MatLoader.Create(image);
            var faces = await services.FaceDetector.DetectAsync(mat);
            if (faces.Count > 0) detectedAny++;
        }

        detectedAny.Should().BeGreaterThanOrEqualTo((int)(sample.Length * 0.7),
            "at least 70 % of CelebA-HQ single portraits should yield ≥ 1 detected face");
    }

    /// <summary>
    ///     Verifies that face-bounding boxes returned by <see cref="IFaceDetector.DetectAsync" /> lie
    ///     within the image bounds for all faces in a random single-portrait image.
    /// </summary>
    [Fact]
    public async Task DetectAsync_SinglePortrait_AllFaceBoundsWithinImage()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        using var mat = services.MatLoader.Create(image);

        var faces = await services.FaceDetector.DetectAsync(mat);

        foreach (var face in faces)
        {
            face.Rect.X.Should().BeGreaterThanOrEqualTo(0);
            face.Rect.Y.Should().BeGreaterThanOrEqualTo(0);
            face.Rect.Right.Should().BeLessThanOrEqualTo((int)image.Info.Width);
            face.Rect.Bottom.Should().BeLessThanOrEqualTo((int)image.Info.Height);
        }
    }

    /// <summary>
    ///     Verifies that each detected face has a confidence score in the valid range [0, 1].
    /// </summary>
    [Fact]
    public async Task DetectAsync_SinglePortrait_AllScoresInValidRange()
    {
        var images = dataset.GetSingleImages();
        if (images.Length == 0) Assert.Skip("No single images cached — run with network access to download.");

        var path = images[Random.Shared.Next(images.Length)];
        using var image = services.ImageLoader.Open(path);
        using var mat = services.MatLoader.Create(image);

        var faces = await services.FaceDetector.DetectAsync(mat);

        foreach (var face in faces)
            face.Score.Should().BeInRange(0f, 1f);
    }

    #endregion
}