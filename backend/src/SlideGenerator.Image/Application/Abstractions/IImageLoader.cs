/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IImageLoader.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Application.Abstractions;

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
    ///     Retrieves metadata about an image, such as its dimensions, from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the image.</param>
    /// <returns>An <see cref="IImageInfo" /> instance containing metadata about the image.</returns>
    IImageInfo GetInfo(string path);

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
}