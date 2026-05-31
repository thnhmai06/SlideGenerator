/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IMatFactory.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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