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
///     <see cref="IFaceDetector" />, <see cref="IMatLoader" />, <see cref="IImageLoader" />,
///     and <see cref="IRoiResolver" />.
/// </summary>
/// <remarks>
///     Internal types (<see cref="YuNet" />, <see cref="OpenCvMatLoader" />,
///     <see cref="MagickImageLoader" />) are accessible via the
///     <c>[InternalsVisibleTo("SlideGenerator.Image.Tests")]</c> declaration in
///     <c>SlideGenerator.Image.csproj</c>.
///     The model is loaded from <c>AppContext.BaseDirectory/Infrastructure/Binary/YuNet.onnx</c>,
///     which is copied to the test output directory via an explicit <c>&lt;Content&gt;</c> item
///     in the test <c>.csproj</c> — avoiding the race hazard of <c>Directory.SetCurrentDirectory</c>.
/// </remarks>
public sealed class ImageServiceFixture : IDisposable
{
    private static readonly string ModelPath =
        Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Binary", "YuNet.onnx");

    /// <summary>
    ///     Initializes the fixture, creating real OpenCV and Magick.NET service instances.
    /// </summary>
    public ImageServiceFixture()
    {
        MatLoader = new OpenCvMatLoader();
        ImageLoader = new MagickImageLoader();

        FaceDetector = new YuNet();

        RoiResolver = new RoiResolver(FaceDetector, MatLoader, NullLogger<RoiResolver>.Instance);
    }

    /// <summary>Real YuNet face detector backed by YuNet.onnx.</summary>
    public IFaceDetector FaceDetector { get; }

    /// <summary>Real OpenCV Mat factory.</summary>
    public IMatLoader MatLoader { get; }

    /// <summary>Real Magick.NET image factory.</summary>
    public IImageLoader ImageLoader { get; }

    /// <summary>Real ROI resolver wired to <see cref="FaceDetector" /> and <see cref="MatLoader" />.</summary>
    public IRoiResolver RoiResolver { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        FaceDetector.Dispose();
    }
}