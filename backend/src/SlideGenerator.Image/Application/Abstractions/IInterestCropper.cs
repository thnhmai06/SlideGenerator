/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IInterestCropper.cs
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
///     Performs content-aware (interest-based) image cropping using a third-party library.
/// </summary>
public interface IInterestCropper
{
    /// <summary>
    ///     Crops and resizes <paramref name="image" /> to <paramref name="targetSize" /> using the
    ///     specified <paramref name="mode" /> to select the region of interest.
    ///     Returns <see langword="null" /> when <paramref name="targetSize" /> has a zero or
    ///     negative dimension.
    /// </summary>
    /// <param name="image">Source image. Not modified.</param>
    /// <param name="targetSize">Desired output dimensions.</param>
    /// <param name="mode">Interest strategy.</param>
    /// <returns>
    ///     A new <see cref="IImage" /> cropped to <paramref name="targetSize" />, or
    ///     <see langword="null" /> for trivial inputs.
    /// </returns>
    IImage? Crop(IImage image, Size targetSize, InterestType mode);
}