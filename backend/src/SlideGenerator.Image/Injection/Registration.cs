/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Registration.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Services;
using SlideGenerator.Image.Domain;
using SlideGenerator.Image.Infrastructure;
using SlideGenerator.Image.Infrastructure.Adapters;
using SlideGenerator.Image.Infrastructure.Services;

namespace SlideGenerator.Image.Injection;

public static class Registration
{
    private const string ModelPath = @"Infrastructure\Binary\YuNet.onnx";

    public static IServiceCollection AddImageServices(this IServiceCollection services)
    {
        services.AddSingleton<IImageFactory, MagickImageFactory>();
        services.AddSingleton<IMatFactory, OpenCvMatFactory>();

        services.AddSingleton<IFaceDetector>(_ =>
            new YuNet(
                FaceDetectorYN.Create(ModelPath, string.Empty,
                    Rules.FaceInputSize.ToOpenCv(), Rules.FaceConfidence), Rules.FaceInputSize.ToOpenCv()));
        services.AddSingleton<IRoiResolver>(sp => new RoiResolver(
            sp.GetRequiredService<IFaceDetector>(),
            sp.GetRequiredService<IMatFactory>(),
            sp.GetService<ILogger<RoiResolver>>()));
        return services;
    }
}