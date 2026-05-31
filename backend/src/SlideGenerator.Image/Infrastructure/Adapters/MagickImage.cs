/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: MagickImage.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using ImageMagick;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Infrastructure.Adapters;

/// <summary>
///     Adapter for <see cref="ImageMagick.MagickImage" /> implementing <see cref="IImage" />.
/// </summary>
internal sealed class MagickImage(ImageMagick.MagickImage image) : IImage
{
    public IImageInfo Info { get; } = new ImageInfo(image);

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

    public byte[] ToBytes()
    {
        return image.ToByteArray(MagickFormat.Png);
    }

    public void Dispose()
    {
        image.Dispose();
    }

    object ICloneable.Clone()
    {
        return new MagickImage(new ImageMagick.MagickImage(image));
    }

    private class ImageInfo(ImageMagick.MagickImage image) : IImageInfo
    {
        public uint Width => image.Width;
        public uint Height => image.Height;
    }
}