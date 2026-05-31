/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IImage.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;

namespace SlideGenerator.Image.Domain.Entities;

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
    ///     Crops the image to the specified region.
    /// </summary>
    /// <param name="region">The rectangle defining the region to keep.</param>
    void Crop(Rectangle region);

    /// <summary>
    ///     Resizes the image to the specified dimensions.
    /// </summary>
    /// <param name="size">The target size.</param>
    void Resize(Size size);

    /// <summary>
    ///     Writes the image to the specified file path asynchronously.
    /// </summary>
    /// <param name="path">The target file path.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task ToFileAsync(string path);

    /// <summary>
    ///     Converts the image to a byte array in PNG format.
    /// </summary>
    /// <returns>A byte array containing the image data in PNG format.</returns>
    byte[] ToBytes();
}