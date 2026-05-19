/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IMatFactory.cs
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
///     Defines a factory for creating <see cref="IMat" /> instances.
/// </summary>
public interface IMatFactory
{
    /// <summary>
    ///     Decodes the specified byte array into an <see cref="IMat" />.
    /// </summary>
    /// <param name="data">The raw image data bytes.</param>
    /// <returns>A new <see cref="IMat" /> instance.</returns>
    IMat Create(byte[] data);

    /// <summary>
    ///     Decodes the specified <see cref="IImage" /> into an <see cref="IMat" />.
    /// </summary>
    /// <param name="image">The source image.</param>
    /// <returns>A new <see cref="IMat" /> instance.</returns>
    IMat Create(IImage image);

    /// <summary>
    ///     Creates an empty <see cref="IMat" />.
    /// </summary>
    /// <returns>A new <see cref="IMat" /> instance.</returns>
    IMat Empty();
}