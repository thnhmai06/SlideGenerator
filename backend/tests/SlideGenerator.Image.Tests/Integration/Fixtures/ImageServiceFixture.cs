/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: ImageServiceFixture.cs
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

using Microsoft.Extensions.Logging.Abstractions;
using OpenCvSharp;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Services;
using SlideGenerator.Image.Domain;
using SlideGenerator.Image.Infrastructure;
using SlideGenerator.Image.Infrastructure.Adapters;
using SlideGenerator.Image.Infrastructure.Services;

namespace SlideGenerator.Image.Tests.Integration.Fixtures;

/// <summary>
///     xUnit v3 collection fixture providing fully-wired, real implementations of
///     <see cref="IFaceDetector" />, <see cref="IMatFactory" />, <see cref="IImageFactory" />,
///     and <see cref="IRoiResolver" />.
/// </summary>
/// <remarks>
///     Internal types (<see cref="YuNet" />, <see cref="OpenCvMatFactory" />,
///     <see cref="MagickImageFactory" />) are accessible via the
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
        MatFactory = new OpenCvMatFactory();
        ImageFactory = new MagickImageFactory();

        var inputSize = Rules.FaceInputSize.ToOpenCv();
        var core = FaceDetectorYN.Create(ModelPath, string.Empty, inputSize, Rules.FaceConfidence);
        FaceDetector = new YuNet(core, inputSize);

        RoiResolver = new RoiResolver(FaceDetector, MatFactory, NullLogger<RoiResolver>.Instance);
    }

    /// <summary>Real YuNet face detector backed by YuNet.onnx.</summary>
    public IFaceDetector FaceDetector { get; }

    /// <summary>Real OpenCV Mat factory.</summary>
    public IMatFactory MatFactory { get; }

    /// <summary>Real Magick.NET image factory.</summary>
    public IImageFactory ImageFactory { get; }

    /// <summary>Real ROI resolver wired to <see cref="FaceDetector" /> and <see cref="MatFactory" />.</summary>
    public IRoiResolver RoiResolver { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        FaceDetector.Dispose();
    }
}