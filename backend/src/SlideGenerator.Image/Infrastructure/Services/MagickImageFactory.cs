/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: MagickImageFactory.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using MagickImage = SlideGenerator.Image.Infrastructure.Adapters.MagickImage;
using MagickImageInfo = SlideGenerator.Image.Infrastructure.Adapters.MagickImageInfo;

namespace SlideGenerator.Image.Infrastructure.Services;

internal sealed class MagickImageFactory : IImageFactory
{
    public IImage Open(string path)
    {
        return new MagickImage(new ImageMagick.MagickImage(path));
    }

    public IImageInfo GetInfo(string path)
    {
        return new MagickImageInfo(new ImageMagick.MagickImageInfo(path));
    }

    public bool TryGetInfo(string path, [MaybeNullWhen(false)] out IImageInfo info)
    {
        try
        {
            info = GetInfo(path);
            return true;
        }
        catch
        {
            info = null;
            return false;
        }
    }
}