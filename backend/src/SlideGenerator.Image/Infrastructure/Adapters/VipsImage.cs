/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: VipsImage.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using SlideGenerator.Image.Domain.Entities;
using NetVipsEnums = NetVips.Enums;
using NetVipsImage = NetVips.Image;
using Size = System.Drawing.Size;

namespace SlideGenerator.Image.Infrastructure.Adapters;

/// <summary>
///     Adapter wrapping a <see cref="NetVipsImage" /> that implements <see cref="IImage" />.
/// </summary>
internal sealed class VipsImage(NetVipsImage core) : IImage
{
    internal NetVipsImage Native => core;

    public IImageInfo Info => new VipsImageInfo(core);

    public IImage Crop(Rectangle r)
    {
        return new VipsImage(core.ExtractArea(r.X, r.Y, r.Width, r.Height));
    }

    public IImage Resize(Size s)
    {
        return new VipsImage(core.ThumbnailImage(s.Width, s.Height, NetVipsEnums.Size.Force));
    }

    public void ToPng(string path)
    {
        core.WriteToFile(path);
    }

    public byte[] ToPng()
    {
        return core.WriteToBuffer(".png");
    }

    public void Dispose()
    {
        core.Dispose();
    }

    object ICloneable.Clone()
    {
        return new VipsImage(core.Copy());
    }

    private sealed class VipsImageInfo(NetVipsImage image) : IImageInfo
    {
        public uint Width => (uint)image.Width;
        public uint Height => (uint)image.Height;
    }
}