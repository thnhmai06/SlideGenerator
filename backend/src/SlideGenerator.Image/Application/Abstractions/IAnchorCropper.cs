/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IAnchorCropper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Application.Abstractions;

/// <summary>
///     Performs anchor-based image cropping, optionally using face detection to locate the anchor point.
/// </summary>
public interface IAnchorCropper
{
    /// <summary>
    ///     Crops <paramref name="image" /> to <paramref name="targetSize" /> using the anchor strategy
    ///     defined by <paramref name="option" />.
    /// </summary>
    /// <param name="image">Source image. Not modified.</param>
    /// <param name="targetSize">Desired output dimensions.</param>
    /// <param name="option">Anchor strategy and pivot.</param>
    /// <returns>
    ///     A new <see cref="IImage" />, or <see langword="null" /> if the anchor could not be resolved
    ///     (e.g., no face detected for a face-based anchor).
    /// </returns>
    ValueTask<IImage?> CropAsync(IImage image, Size targetSize, AnchorOption option);
}