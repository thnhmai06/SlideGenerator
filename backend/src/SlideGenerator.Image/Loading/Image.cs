/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Image.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using NetVipsEnums = NetVips.Enums;
using NetVipsImage = NetVips.Image;
using Size = System.Drawing.Size;

namespace SlideGenerator.Image.Loading;

/// <summary>
///     Represents metadata of an image, including its dimensions.
/// </summary>
public interface IImageInfo
{
    /// <summary>
    ///     Gets the width of the image.
    /// </summary>
    uint Width { get; }

    /// <summary>
    ///     Gets the height of the image.
    /// </summary>
    uint Height { get; }
}

/// <summary>
///     Represents an abstract image that can be manipulated and queried.
/// </summary>
/// <remarks>
///     This interface decouples the core logic from specific image processing libraries like Magick.NET.
/// </remarks>
public interface IImage : IDisposable, ICloneable
{
    IImageInfo Info { get; }

    /// <summary>
    ///     Returns a new image cropped to the specified region. The original image is not modified.
    /// </summary>
    /// <param name="region">The rectangle defining the region to keep.</param>
    /// <returns>A new <see cref="IImage" /> containing the cropped result.</returns>
    IImage Crop(Rectangle region);

    /// <summary>
    ///     Returns a new image resized to the specified dimensions. The original image is not modified.
    /// </summary>
    /// <param name="size">The target size.</param>
    /// <returns>A new <see cref="IImage" /> containing the resized result.</returns>
    IImage Resize(Size size);

    /// <summary>
    ///     Writes the image to the specified .png file path asynchronously.
    /// </summary>
    /// <param name="path">The target file path.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    void ToPng(string path);

    /// <summary>
    ///     Converts the image to a byte array in PNG format.
    /// </summary>
    /// <returns>A byte array containing the image data in PNG format.</returns>
    byte[] ToPng();
}

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
