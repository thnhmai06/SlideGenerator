/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: OpenCvMatFactory.cs
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

using OpenCvSharp;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Infrastructure.Adapters;

namespace SlideGenerator.Image.Infrastructure.Services;

public sealed class OpenCvMatFactory : IMatFactory
{
    public IMat Create(byte[] data)
    {
        return new OpenCvMat(Mat.FromImageData(data));
    }

    public IMat Create(IImage image)
    {
        if (image is not MagickImage adapter)
            throw new ArgumentException($"IImage must be an instance of {nameof(MagickImage)}.", nameof(image));

        var bytes = adapter.ToByteArray();
        return Create(bytes);
    }

    public IMat Empty()
    {
        return new OpenCvMat(new Mat());
    }
}