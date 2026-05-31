/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: OpenCvMatLoader.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using OpenCvSharp;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Infrastructure.Adapters;

namespace SlideGenerator.Image.Infrastructure.Services;

internal sealed class OpenCvMatLoader : IMatLoader
{
    public IMat Create(byte[] data)
    {
        return new OpenCvMat(Mat.FromImageData(data));
    }

    public IMat Create(IImage image)
    {
        return Create(image.ToBytes());
    }

    public IMat Empty()
    {
        return new OpenCvMat(new Mat());
    }
}