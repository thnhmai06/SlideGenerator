/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: ImageServiceFixture.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Services;
using SlideGenerator.Image.Infrastructure.Adapters;
using SlideGenerator.Image.Infrastructure.Services;

namespace SlideGenerator.Image.Tests.Integration.Fixtures;

/// <summary>
///     xUnit v3 collection fixture providing fully-wired, real implementations of
///     <see cref="IFaceDetector" />, <see cref="IImageLoader" />, <see cref="IAnchorCropper" />,
///     <see cref="IInterestCropper" />, and <see cref="ISmartCropper" />.
/// </summary>
public sealed class ImageServiceFixture : IDisposable
{
    /// <summary>
    ///     Initializes the fixture, creating real OpenCV and NetVips service instances.
    /// </summary>
    public ImageServiceFixture()
    {
        ImageLoader = new VipsImageLoader();
        FaceDetector = new YuNet();
        AnchorCropper = new AnchorCropper(FaceDetector);
        InterestCropper = new LibvipsInterestCropper();
        Cropper = new SmartCropper(AnchorCropper, InterestCropper, NullLogger<SmartCropper>.Instance);
    }

    /// <summary>Real YuNet face detector backed by YuNet.onnx.</summary>
    public IFaceDetector FaceDetector { get; }

    /// <summary>Real NetVips image factory.</summary>
    public IImageLoader ImageLoader { get; }

    /// <summary>Real anchor-based cropper wired to <see cref="FaceDetector" />.</summary>
    public IAnchorCropper AnchorCropper { get; }

    /// <summary>Real libvips content-aware cropper.</summary>
    public IInterestCropper InterestCropper { get; }

    /// <summary>Real smart cropper wired to <see cref="AnchorCropper" /> and <see cref="InterestCropper" />.</summary>
    public ISmartCropper Cropper { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        FaceDetector.Dispose();
    }
}