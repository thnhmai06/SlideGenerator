/*
 * Copyright (C) 2026 Thành Mai
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
using SlideGenerator.Image.Entities.Detectors;
using SlideGenerator.Image.Services;

namespace SlideGenerator.Image;

public static class Registration
{
    private const string ModelPath = @"Binary\YuNet.onnx";
    private const float Confidence = 0.8f;
    private static readonly Size InputSize = new(640, 640);

    public static IServiceCollection AddImageServices(this IServiceCollection services)
    {
        services.AddSingleton<FaceDetector>(_ =>
            new YuNet(
                FaceDetectorYN.Create(ModelPath, string.Empty, InputSize, Confidence),
                InputSize));
        services.AddSingleton<RoiResolver>();
        return services;
    }
}