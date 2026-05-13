/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: MagickImage.cs
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

using System.Drawing;
using ImageMagick;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Infrastructure.Adapters;

/// <summary>
///     Adapter for <see cref="ImageMagick.MagickImage" /> implementing <see cref="IImage" />.
/// </summary>
public sealed class MagickImage(ImageMagick.MagickImage image) : IImage
{
    public uint Width => image.Width;
    public uint Height => image.Height;

    public void Crop(Rectangle region)
    {
        image.Crop(new MagickGeometry(region.X, region.Y, (uint)region.Width, (uint)region.Height));
    }

    public void Resize(Size size)
    {
        image.Resize(new MagickGeometry((uint)size.Width, (uint)size.Height));
    }

    public Task WriteAsync(string path)
    {
        return image.WriteAsync(path);
    }

    public byte[] ToByteArray()
    {
        return image.ToByteArray();
    }

    public byte[] ToPngByteArray()
    {
        return image.ToByteArray(MagickFormat.Png);
    }

    public IImage Clone()
    {
        return new MagickImage(new ImageMagick.MagickImage(image));
    }

    public void Dispose()
    {
        image.Dispose();
    }
}