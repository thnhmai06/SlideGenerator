/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: RoiResolverTests.cs
 */

using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Moq;
using OpenCvSharp;
using SlideGenerator.Image.Entities.Detectors;
using SlideGenerator.Image.Models;
using SlideGenerator.Image.Models.Options;
using SlideGenerator.Image.Services;
using Xunit;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace SlideGenerator.Tests.Image;

public sealed class RoiResolverTests
{
    private readonly Mock<FaceDetector> _faceDetectorMock;
    private readonly Mock<ILogger<RoiResolver>> _loggerMock;
    private readonly RoiResolver _resolver;

    public RoiResolverTests()
    {
        _faceDetectorMock = new Mock<FaceDetector>();
        _loggerMock = new Mock<ILogger<RoiResolver>>();
        _resolver = new RoiResolver(_faceDetectorMock.Object, _loggerMock.Object);
    }

    [Fact(Skip = "NATIVE: Requires OpenCV native libraries (OpenCvSharpExtern)")]
    public async Task CalculateRoiAsync_ShouldCenterCrop_WhenNoFacesDetected()
    {
        // Arrange
        using var image = new MagickImage(MagickColors.Black, 100, 100);
        var targetSize = new Size(50, 50);
        var option = new CenterOption { UseFaceAlignment = true };
        
        _faceDetectorMock.Setup(d => d.DetectAsync(It.IsAny<Mat>()))
            .ReturnsAsync(new List<Face>());

        // Act
        var result = await _resolver.CalculateRoiAsync(image, targetSize, option);

        // Assert
        result.Should().Be(new Rectangle(25, 25, 50, 50));
    }

    [Fact(Skip = "NATIVE: Requires OpenCV native libraries (OpenCvSharpExtern)")]
    public async Task CalculateRoiAsync_ShouldCenterAroundFace_WhenFaceDetected()
    {
        // Arrange
        using var image = new MagickImage(MagickColors.Black, 100, 100);
        var targetSize = new Size(40, 40);
        var option = new CenterOption { UseFaceAlignment = true };
        
        // Face at (10, 10) with size (20, 20) -> Center at (20, 20)
        var face = new Face(new Rectangle(10, 10, 20, 20), 0.99f);
        _faceDetectorMock.Setup(d => d.DetectAsync(It.IsAny<Mat>()))
            .ReturnsAsync(new List<Face> { face });

        // Act
        var result = await _resolver.CalculateRoiAsync(image, targetSize, option);

        // Assert
        // Center is (20, 20). Crop 40x40 around it -> (20-20, 20-20) = (0, 0)
        result.Should().Be(new Rectangle(0, 0, 40, 40));
    }

    [Fact(Skip = "NATIVE: Requires OpenCV native libraries (OpenCvSharpExtern)")]
    public async Task CalculateRoiAsync_ShouldClampToEdge_WhenFaceIsNearEdge()
    {
        // Arrange
        using var image = new MagickImage(MagickColors.Black, 100, 100);
        var targetSize = new Size(40, 40);
        var option = new CenterOption { UseFaceAlignment = true };
        
        // Face at (0, 0) -> Center at (0, 0)
        var face = new Face(new Rectangle(0, 0, 10, 10), 0.99f);
        _faceDetectorMock.Setup(d => d.DetectAsync(It.IsAny<Mat>()))
            .ReturnsAsync(new List<Face> { face });

        // Act
        var result = await _resolver.CalculateRoiAsync(image, targetSize, option);

        // Assert
        // Target center (5, 5). Crop 40x40 would be (-15, -15, 40, 40), clamped to (0, 0, 40, 40)
        result.X.Should().BeGreaterOrEqualTo(0);
        result.Y.Should().BeGreaterOrEqualTo(0);
        result.Width.Should().Be(40);
        result.Height.Should().Be(40);
    }

    [Fact]
    public async Task CalculateRoiAsync_ShouldMaintainAspectRatio_OnSquareImage()
    {
        // Arrange
        using var image = new MagickImage(MagickColors.Black, 100, 100);
        var targetSize = new Size(160, 90); // 16:9 ratio
        var option = new CenterOption { UseFaceAlignment = false };

        // Act
        var result = await _resolver.CalculateRoiAsync(image, targetSize, option);

        // Assert
        // boundedSize in Utilities.cs will be Min(160, 100) and Min(90, 100) -> 100x90
        result.Width.Should().Be(100);
        result.Height.Should().Be(90);
    }
}
