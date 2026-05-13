/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IImageFactory.cs
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
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Application.Abstractions;

/// <summary>
///     Defines a factory for creating <see cref="IImage" /> instances.
/// </summary>
public interface IImageFactory
{
    /// <summary>
    ///     Decodes the specified byte array into an <see cref="IImage" />.
    /// </summary>
    /// <param name="data">The raw image data bytes.</param>
    /// <returns>A new <see cref="IImage" /> instance.</returns>
    IImage Open(byte[] data);

    /// <summary>
    ///     Loads an <see cref="IImage" /> from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the image.</param>
    /// <returns>A new <see cref="IImage" /> instance.</returns>
    IImage Open(string path);

    /// <summary>
    ///     Converts an <see cref="IMat" /> to an <see cref="IImage" />.
    /// </summary>
    /// <param name="mat">The image matrix.</param>
    /// <returns>A new <see cref="IImage" /> instance.</returns>
    IImage Open(IMat mat);
}






