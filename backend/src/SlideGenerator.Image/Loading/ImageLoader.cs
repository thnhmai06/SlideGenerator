/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: ImageLoader.cs
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

/// <summary>
///     Defines a factory for creating <see cref="IImage" /> instances.
/// </summary>
public interface IImageLoader
{
    /// <summary>
    ///     Loads an <see cref="IImage" /> from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the image.</param>
    /// <returns>A new <see cref="IImage" /> instance.</returns>
    IImage Open(string path);

    /// <summary>
    ///     Loads an <see cref="IImage" /> from an in-memory buffer, without touching disk.
    /// </summary>
    /// <param name="data">The raw encoded image bytes.</param>
    /// <returns>A new <see cref="IImage" /> instance.</returns>
    IImage Open(byte[] data);

    /// <summary>
    ///     Retrieves metadata about an image, such as its dimensions, from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the image.</param>
    /// <returns>An <see cref="IImageInfo" /> instance containing metadata about the image.</returns>
    IImageInfo GetInfo(string path);

    /// <summary>
    ///     Retrieves metadata about an image, such as its dimensions, from an in-memory buffer.
    /// </summary>
    /// <param name="data">The raw encoded image bytes.</param>
    /// <returns>An <see cref="IImageInfo" /> instance containing metadata about the image.</returns>
    IImageInfo GetInfo(byte[] data);

    /// <summary>
    ///     Attempts to retrieve metadata about an image from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the image.</param>
    /// <param name="info">
    ///     When this method returns, contains an <see cref="IImageInfo" /> instance containing metadata about the image,
    ///     if the operation was successful; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the metadata was successfully retrieved; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetInfo(string path, [MaybeNullWhen(false)] out IImageInfo info);

    /// <summary>
    ///     Attempts to retrieve metadata about an image from an in-memory buffer.
    /// </summary>
    /// <param name="data">The raw encoded image bytes.</param>
    /// <param name="info">
    ///     When this method returns, contains an <see cref="IImageInfo" /> instance containing metadata about the image,
    ///     if the operation was successful; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the metadata was successfully retrieved; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetInfo(byte[] data, [MaybeNullWhen(false)] out IImageInfo info);
}

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
