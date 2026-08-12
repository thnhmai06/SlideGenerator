/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: VipsImageLoader.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Diagnostics.CodeAnalysis;
using NetVipsImage = NetVips.Image;

namespace SlideGenerator.Image.Loading;

internal sealed class VipsImageLoader : IImageLoader
{
    public IImage Open(string path)
    {
        return new VipsImage(NetVipsImage.NewFromFile(path));
    }

    public IImage Open(byte[] data)
    {
        return new VipsImage(NetVipsImage.NewFromBuffer(data));
    }

    public IImageInfo GetInfo(string path)
    {
        using var img = NetVipsImage.NewFromFile(path);
        return new SizeInfo((uint)img.Width, (uint)img.Height);
    }

    public IImageInfo GetInfo(byte[] data)
    {
        using var img = NetVipsImage.NewFromBuffer(data);
        return new SizeInfo((uint)img.Width, (uint)img.Height);
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

    public bool TryGetInfo(byte[] data, [MaybeNullWhen(false)] out IImageInfo info)
    {
        try
        {
            info = GetInfo(data);
            return true;
        }
        catch
        {
            info = null;
            return false;
        }
    }

    private sealed record SizeInfo(uint Width, uint Height) : IImageInfo;
}