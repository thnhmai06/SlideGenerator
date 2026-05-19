/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Registration.cs
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

using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<IRoiResolver, RoiResolver>();
        return services;
    }
}