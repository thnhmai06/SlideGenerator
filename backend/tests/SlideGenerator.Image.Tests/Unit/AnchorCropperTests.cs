/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: AnchorCropperTests.cs
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
using NSubstitute;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Application.Services;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Domain.Models;
using Xunit;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="AnchorCropper" />, verifying that it passes the correct crop
///     rectangle to <see cref="IImage.Crop" /> for each <see cref="AnchorType" /> and that
///     face detection is only triggered for face-based anchors.
/// </summary>
public sealed class AnchorCropperTests
{
    private readonly AnchorCropper _cropper;
    private readonly IFaceDetector _faceDetector = Substitute.For<IFaceDetector>();

    public AnchorCropperTests()
    {
        _cropper = new AnchorCropper(_faceDetector);
    }

    #region ROI bounds invariant

    /// <summary>
    ///     Verifies that the crop rectangle passed to <see cref="IImage.Crop" />
    ///     always lies entirely within the source image bounds.
    /// </summary>
    [Theory]
    [InlineData(300, 200, 100, 100)]
    [InlineData(50, 50, 200, 200)]
    [InlineData(400, 300, 400, 300)]
    public async Task CropAsync_AnyImageSize_CropRectAlwaysWithinImageBounds(
        uint imgW, uint imgH, int targetW, int targetH)
    {
        SetFaces();
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult, imgW, imgH,
            (uint)Math.Min(targetW, (int)imgW), (uint)Math.Min(targetH, (int)imgH));

        await _cropper.CropAsync(source, new Size(targetW, targetH),
            new AnchorOption { Type = AnchorType.Image });

        source.Received(1).Crop(Arg.Is<Rectangle>(r =>
            r.X >= 0 && r.Y >= 0 &&
            r.Right <= (int)imgW && r.Bottom <= (int)imgH));
    }

    #endregion

    #region AnchorType.Eyes

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Eyes" /> with rule-of-thirds pivot calls
    ///     <see cref="IImage.Crop" /> at the eye centroid.
    /// </summary>
    [Fact]
    public async Task CropAsync_EyesAnchorRuleOfThirdsPivot_FaceWithEyes_CropCalledAtEyesCenter()
    {
        // RightEye=(110,115), LeftEye=(135,115) → EyesCenter=(122,115)
        SetFaces(new Face(
            new Rectangle(100, 100, 60, 60), 0.9f,
            new Point(110, 115),
            new Point(135, 115)));
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Eyes, Pivot = new Vector2(0.5f, 1 / 3f) });

        // anchor=(122,115); pivot=(0.5,1/3): x=round(122-50)=72, y=round(115-33.33)=82
        source.Received(1).Crop(Arg.Is<Rectangle>(r =>
            r.X == 72 && r.Y == 82 && r.Width == 100 && r.Height == 100));
    }

    #endregion

    #region AnchorType.Nose

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Nose" /> calls <see cref="IImage.Crop" />
    ///     at the nose landmark.
    /// </summary>
    [Fact]
    public async Task CropAsync_NoseAnchor_FaceWithNose_CropCalledAtNose()
    {
        // Face rect (100,100,50,50), Nose=(120,130)
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f, Nose: new Point(120, 130)));
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Nose });

        // anchor=(120,130); pivot=(0.5,0.5): x=120-50=70, y=130-50=80
        source.Received(1).Crop(new Rectangle(70, 80, 100, 100));
    }

    #endregion

    #region AnchorType.Mouth

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Mouth" /> calls <see cref="IImage.Crop" />
    ///     at the mouth midpoint.
    /// </summary>
    [Fact]
    public async Task CropAsync_MouthAnchor_FaceWithMouth_CropCalledAtMouthCenter()
    {
        // RightMouth=(110,140), LeftMouth=(130,140) → MouthCenter=(120,140)
        SetFaces(new Face(
            new Rectangle(100, 100, 50, 50), 0.9f,
            RightMouth: new Point(110, 140),
            LeftMouth: new Point(130, 140)));
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Mouth });

        // anchor=(120,140); pivot=(0.5,0.5): x=120-50=70, y=140-50=90
        source.Received(1).Crop(new Rectangle(70, 90, 100, 100));
    }

    #endregion

    #region Multi-face centroid

    /// <summary>
    ///     Verifies that when two faces are present, <see cref="AnchorType.Face" /> crops at the
    ///     centroid of the two face centres.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceAnchor_TwoFaces_CropAtCentroid()
    {
        // Face1: rect(100,100,50,50) → center=(125,125)
        // Face2: rect(200,100,50,50) → center=(225,125)
        // centroid = ((125+225)/2, (125+125)/2) = (175,125)
        SetFaces(
            new Face(new Rectangle(100, 100, 50, 50), 0.9f),
            new Face(new Rectangle(200, 100, 50, 50), 0.9f));
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Face });

        // anchor=(175,125); pivot=(0.5,0.5): x=175-50=125, y=125-50=75
        source.Received(1).Crop(new Rectangle(125, 75, 100, 100));
    }

    #endregion

    #region Non-zero Ratio

    /// <summary>
    ///     Verifies that a non-zero <see cref="AnchorOption.Ratio" /> shifts the anchor by
    ///     the face-rect dimensions for <see cref="AnchorType.Face" />.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceAnchor_NonZeroRatio_AnchorOffsetByFaceSize()
    {
        // Face: rect(100,100,60,60) → center=(130,130), avgW=avgH=60
        // Ratio=(1,0): anchor = (round(130 + 1*60), round(130 + 0*60)) = (190, 130)
        SetFaces(new Face(new Rectangle(100, 100, 60, 60), 0.9f));
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Face, Ratio = new Vector2(1, 0) });

        // anchor=(190,130); pivot=(0.5,0.5): x=190-50=140, y=130-50=80
        source.Received(1).Crop(new Rectangle(140, 80, 100, 100));
    }

    #endregion

    #region Resize aspect-ratio preservation

    /// <summary>
    ///     Verifies that <see cref="IImage.Resize" /> receives a size computed by
    ///     <see cref="Application.Utilities.GetMaxAspectSize" /> rather than the raw target size,
    ///     preserving the target aspect ratio within the cropped region.
    /// </summary>
    [Fact]
    public async Task CropAsync_ImageAnchor_ResizeCalledWithAspectPreservingSize()
    {
        // Source 400×300, target 200×100 (2:1 ratio), crop result 200×200 (square)
        // GetMaxAspectSize((200,200), (200,100)): orig=1.0, ratio=2.0
        //   1.0 < 2.0 → width=200, height=round(200/2.0)=100 → (200,100)
        // Resize should be called with (200,100), not (200,200)
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        cropResult.Info.Width.Returns(200u);
        cropResult.Info.Height.Returns(200u);
        cropResult.Resize(Arg.Any<Size>()).Returns(resizeResult);

        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(400u);
        source.Info.Height.Returns(300u);
        source.Crop(Arg.Any<Rectangle>()).Returns(cropResult);

        await _cropper.CropAsync(source, new Size(200, 100),
            new AnchorOption { Type = AnchorType.Image });

        cropResult.Received(1).Resize(new Size(200, 100));
    }

    #endregion

    #region Helpers

    /// <summary>
    ///     Builds a mock source image whose <see cref="IImage.Crop" /> returns
    ///     <paramref name="cropResult" /> whose <see cref="IImage.Resize" /> returns
    ///     <paramref name="resizeResult" />.
    /// </summary>
    private static IImage FakeSource(
        IImage cropResult, IImage resizeResult,
        uint imgW = 300, uint imgH = 200,
        uint cropW = 100, uint cropH = 100)
    {
        cropResult.Info.Width.Returns(cropW);
        cropResult.Info.Height.Returns(cropH);
        cropResult.Resize(Arg.Any<Size>()).Returns(resizeResult);

        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(imgW);
        source.Info.Height.Returns(imgH);
        source.Crop(Arg.Any<Rectangle>()).Returns(cropResult);
        return source;
    }

    private void SetFaces(params Face[] faces)
    {
        _faceDetector.DetectAsync(Arg.Any<IImage>())
            .Returns(Task.FromResult<IReadOnlyList<Face>>(faces));
    }

    #endregion

    #region Trivial inputs

    /// <summary>
    ///     Verifies that a zero target width returns <see langword="null" /> without calling face
    ///     detection or crop.
    /// </summary>
    [Fact]
    public async Task CropAsync_ZeroTargetWidth_ReturnsNull()
    {
        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(300u);
        source.Info.Height.Returns(200u);

        var result = await _cropper.CropAsync(source, new Size(0, 100),
            new AnchorOption { Type = AnchorType.Image });

        result.Should().BeNull();
        await _faceDetector.DidNotReceive().DetectAsync(Arg.Any<IImage>());
        source.DidNotReceive().Crop(Arg.Any<Rectangle>());
    }

    /// <summary>
    ///     Verifies that a zero target height returns <see langword="null" /> without calling face
    ///     detection or crop.
    /// </summary>
    [Fact]
    public async Task CropAsync_ZeroTargetHeight_ReturnsNull()
    {
        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(300u);
        source.Info.Height.Returns(200u);

        var result = await _cropper.CropAsync(source, new Size(100, 0),
            new AnchorOption { Type = AnchorType.Face });

        result.Should().BeNull();
        await _faceDetector.DidNotReceive().DetectAsync(Arg.Any<IImage>());
    }

    #endregion

    #region AnchorType.Image

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Image" /> with default <c>Ratio=(0,0)</c>
    ///     calls <see cref="IImage.Crop" /> at image center and returns the resize result.
    /// </summary>
    [Fact]
    public async Task CropAsync_ImageAnchorDefaultRatio_CropCalledAtImageCenter()
    {
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        var result = await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Image });

        // Image 300x200 → center=(150,100); pivot=(0.5,0.5): x=150-50=100, y=100-50=50
        source.Received(1).Crop(new Rectangle(100, 50, 100, 100));
        result.Should().BeSameAs(resizeResult);
    }

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Image" /> never triggers face detection.
    /// </summary>
    [Fact]
    public async Task CropAsync_ImageAnchorOnly_FaceDetectorNotCalled()
    {
        var source = FakeSource(Substitute.For<IImage>(), Substitute.For<IImage>());

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Image });

        await _faceDetector.DidNotReceive().DetectAsync(Arg.Any<IImage>());
    }

    #endregion

    #region AnchorType.Face

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Face" /> calls <see cref="IImage.Crop" />
    ///     at the face bounding-box center and returns the resize result.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceAnchor_FaceDetected_CropCalledAtFaceCenter()
    {
        // Face at (100,100) with size 50x50 → FaceCenter=(125,125)
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f));
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        var result = await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Face });

        // anchor=(125,125); pivot=(0.5,0.5): x=125-50=75, y=125-50=75
        source.Received(1).Crop(new Rectangle(75, 75, 100, 100));
        result.Should().BeSameAs(resizeResult);
    }

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Face" /> returns <see langword="null" />
    ///     when no faces are detected.
    /// </summary>
    [Fact]
    public async Task CropAsync_FaceAnchor_NoFaceDetected_ReturnsNull()
    {
        SetFaces();
        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(300u);
        source.Info.Height.Returns(200u);

        var result = await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Face });

        result.Should().BeNull();
    }

    #endregion

    #region Missing landmarks → null

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Eyes" /> returns <see langword="null" /> when no
    ///     detected face has eye landmarks (distinct from the no-face case).
    /// </summary>
    [Fact]
    public async Task CropAsync_EyesAnchor_FaceWithNoEyes_ReturnsNull()
    {
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f)); // no eye landmarks
        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(300u);
        source.Info.Height.Returns(200u);

        var result = await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Eyes });

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Nose" /> returns <see langword="null" /> when no
    ///     detected face has a nose landmark.
    /// </summary>
    [Fact]
    public async Task CropAsync_NoseAnchor_FaceWithNoNose_ReturnsNull()
    {
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f)); // no nose
        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(300u);
        source.Info.Height.Returns(200u);

        var result = await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Nose });

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="AnchorType.Mouth" /> returns <see langword="null" /> when no
    ///     detected face has both mouth corner landmarks.
    /// </summary>
    [Fact]
    public async Task CropAsync_MouthAnchor_FaceWithNoMouth_ReturnsNull()
    {
        SetFaces(new Face(new Rectangle(100, 100, 50, 50), 0.9f)); // no mouth
        var source = Substitute.For<IImage>();
        source.Info.Width.Returns(300u);
        source.Info.Height.Returns(200u);

        var result = await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Mouth });

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that when two faces are present but only one has mouth landmarks, the anchor is
    ///     based on the face that has them (partial set is still handled correctly).
    /// </summary>
    [Fact]
    public async Task CropAsync_MouthAnchor_PartialFaces_UsesOnlyFacesWithMouth()
    {
        // Face1: RightMouth=(110,140), LeftMouth=(130,140) → MouthCenter=(120,140)
        // Face2: no mouth landmarks → excluded from Centroid
        SetFaces(
            new Face(new Rectangle(100, 100, 50, 50), 0.9f,
                RightMouth: new Point(110, 140),
                LeftMouth: new Point(130, 140)),
            new Face(new Rectangle(200, 100, 50, 50), 0.9f)); // no mouth
        var cropResult = Substitute.For<IImage>();
        var resizeResult = Substitute.For<IImage>();
        var source = FakeSource(cropResult, resizeResult);

        await _cropper.CropAsync(source, new Size(100, 100),
            new AnchorOption { Type = AnchorType.Mouth });

        // centroid = (120,140); pivot=(0.5,0.5): x=70, y=90
        source.Received(1).Crop(new Rectangle(70, 90, 100, 100));
    }

    #endregion
}