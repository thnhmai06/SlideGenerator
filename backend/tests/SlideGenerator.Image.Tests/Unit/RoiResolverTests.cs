/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: RoiResolverTests.cs
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
using SlideGenerator.Image.Domain.Models;
using Xunit;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="RoiResolver" />, verifying ROI calculation using
///     <see cref="CenterOption" /> and <see cref="RuleOfThirdsOption" /> with and without
///     face detection results. Face detection is provided by a mocked <see cref="IFaceDetector" />.
/// </summary>
public sealed class RoiResolverTests
{
    private readonly IFaceDetector _faceDetector = Substitute.For<IFaceDetector>();
    private readonly IMatFactory _matFactory = Substitute.For<IMatFactory>();
    private readonly RoiResolver _resolver;

    public RoiResolverTests()
    {
        _matFactory.Create(Arg.Any<IImage>()).Returns(Substitute.For<IMat>());
        _resolver = new RoiResolver(_faceDetector, _matFactory, NullLogger<RoiResolver>.Instance);
    }

    #region ROI bounds invariant

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> always produces an ROI rectangle
    ///     that lies entirely within the source image bounds, regardless of the option used.
    /// </summary>
    [Theory]
    [InlineData(300, 200, 100, 100)]
    [InlineData(50, 50, 200, 200)]
    [InlineData(400, 300, 400, 300)]
    public async Task CalculateRoiAsync_AnyOption_RoiAlwaysWithinImageBounds(
        uint imgW, uint imgH, int targetW, int targetH)
    {
        SetFaces();
        using var image = FakeImage(imgW, imgH);
        var option = new CenterOption { UseFaceAlignment = false };

        var roi = await _resolver.CalculateRoiAsync(image, new Size(targetW, targetH), option);

        roi.X.Should().BeGreaterThanOrEqualTo(0);
        roi.Y.Should().BeGreaterThanOrEqualTo(0);
        roi.Right.Should().BeLessThanOrEqualTo((int)imgW);
        roi.Bottom.Should().BeLessThanOrEqualTo((int)imgH);
    }

    #endregion

    #region Helpers

    private static IImage FakeImage(uint width = 300, uint height = 200)
    {
        var image = Substitute.For<IImage>();
        image.Info.Width.Returns(width);
        image.Info.Height.Returns(height);
        return image;
    }

    private void SetFaces(params Face[] faces)
    {
        _faceDetector.DetectAsync(Arg.Any<IMat>())
            .Returns(Task.FromResult<IReadOnlyList<Face>>(faces));
    }

    #endregion

    #region CenterOption — no face alignment

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> with <see cref="CenterOption" />
    ///     and <c>UseFaceAlignment = false</c> produces an ROI centered on the image geometric center.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_CenterOptionNoFaceAlignment_RoiCenteredOnImageCenter()
    {
        using var image = FakeImage();
        var option = new CenterOption { UseFaceAlignment = false };

        var roi = await _resolver.CalculateRoiAsync(image, new Size(100, 100), option);

        // Image center = (150,100); pivot=(0.5,0.5): x=150-50=100, y=100-50=50
        roi.Should().Be(new Rectangle(100, 50, 100, 100));
    }

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> with <see cref="CenterOption" />
    ///     and <c>UseFaceAlignment = true</c> but no detected face falls back to the image geometric center.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_CenterOptionFaceAlignmentNoFaceDetected_RoiCenteredOnImageCenter()
    {
        SetFaces();
        using var image = FakeImage();
        var option = new CenterOption { UseFaceAlignment = true };

        var roi = await _resolver.CalculateRoiAsync(image, new Size(100, 100), option);

        roi.Should().Be(new Rectangle(100, 50, 100, 100));
    }

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> with <see cref="CenterOption" />
    ///     and <c>UseFaceAlignment = true</c> shifts the ROI to the detected face's centroid.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_CenterOptionFaceAlignmentFaceDetected_RoiShiftsToFaceCenter()
    {
        // Face at (100,100) with size 50x50 → FaceCenter = (125,125)
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f));
        using var image = FakeImage();
        var option = new CenterOption { UseFaceAlignment = true };

        var roi = await _resolver.CalculateRoiAsync(image, new Size(100, 100), option);

        // anchor=(125,125); pivot=(0.5,0.5): x=125-50=75, y=125-50=75
        roi.Should().Be(new Rectangle(75, 75, 100, 100));
    }

    #endregion

    #region RuleOfThirdsOption

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> with <see cref="RuleOfThirdsOption" />
    ///     and no detected face produces an ROI centered in the image.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_RuleOfThirdsNoFaceDetected_RoiCenteredInImage()
    {
        SetFaces();
        using var image = FakeImage();
        var option = new RuleOfThirdsOption();

        var roi = await _resolver.CalculateRoiAsync(image, new Size(100, 100), option);

        // No face → CalculateAnchoredRectangle(sourceSize, targetSize) defaults to center/center
        roi.Should().Be(new Rectangle(100, 50, 100, 100));
    }

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> with <see cref="RuleOfThirdsOption" />
    ///     anchors the ROI to the eye-centroid of the detected face when eye landmarks are present.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_RuleOfThirdsFaceWithEyes_RoiAnchoredToEyesCenter()
    {
        // Right eye=(110,115), Left eye=(135,115) → EyesCenter=(122,115)
        SetFaces(new Face(
            new Rectangle(100, 100, 60, 60), 0.9f,
            new Point(110, 115),
            new Point(135, 115)));
        using var image = FakeImage();
        var option = new RuleOfThirdsOption();

        var roi = await _resolver.CalculateRoiAsync(image, new Size(100, 100), option);

        // anchor=(122,115); default pivot=(0.5,1/3): x=round(122-50)=72, y=round(115-33.33)=82
        roi.X.Should().Be(72);
        roi.Y.Should().Be(82);
        roi.Width.Should().Be(100);
        roi.Height.Should().Be(100);
    }

    /// <summary>
    ///     Verifies that <see cref="RoiResolver.CalculateRoiAsync" /> with <see cref="RuleOfThirdsOption" />
    ///     falls back to the face center when eye landmarks are absent.
    /// </summary>
    [Fact]
    public async Task CalculateRoiAsync_RuleOfThirdsFaceWithoutEyes_RoiAnchoredToFaceCenter()
    {
        // No eye landmarks → FaceCenter=(125,125)
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f));
        using var image = FakeImage();
        var option = new RuleOfThirdsOption();

        var roi = await _resolver.CalculateRoiAsync(image, new Size(100, 100), option);

        // anchor=(125,125); pivot=(0.5,1/3): x=75, y=round(125-33.33)=92
        roi.X.Should().Be(75);
        roi.Y.Should().Be(92);
        roi.Width.Should().Be(100);
        roi.Height.Should().Be(100);
    }

    #endregion
}