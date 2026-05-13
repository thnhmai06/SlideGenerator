/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: MagickImageFactory.cs
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
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Infrastructure.Adapters;
using MagickImage = SlideGenerator.Image.Infrastructure.Adapters.MagickImage;

namespace SlideGenerator.Image.Infrastructure.Services;

public sealed class MagickImageFactory : IImageFactory
{
    public IImage Open(byte[] data)
    {
        return new MagickImage(new ImageMagick.MagickImage(data));
    }

    public IImage Open(string path)
    {
        return new MagickImage(new ImageMagick.MagickImage(path));
    }

    public IImage Open(IMat mat)
    {
        if (mat is not OpenCvMat adapter)
            throw new ArgumentException($"IMat must be an instance of {nameof(OpenCvMat)}.", nameof(mat));
        
        var bytes = adapter.Mat.ToBytes();
        return Open(bytes);
    }
}






