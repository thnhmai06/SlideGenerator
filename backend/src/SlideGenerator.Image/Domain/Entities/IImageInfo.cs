/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IImageInfo.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Domain.Entities;

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