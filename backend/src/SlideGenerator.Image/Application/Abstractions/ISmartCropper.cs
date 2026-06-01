/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: ISmartCropper.cs
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
///     Resolves and applies the optimal crop strategy to an image using an ordered fallback
///     chain of <see cref="RoiOption" /> items, returning a new cropped image without
///     modifying the original.
/// </summary>
public interface ISmartCropper
{
    /// <summary>
    ///     Walks <paramref name="options" /> in order and returns the first successful crop as
    ///     a new <see cref="IImage" />. An <see cref="InterestOption" /> always succeeds;
    ///     an <see cref="AnchorOption" /> succeeds when the required landmarks are detected.
    ///     Returns <see langword="null" /> if all options are exhausted without success.
    /// </summary>
    /// <param name="image">Source image — not modified.</param>
    /// <param name="targetSize">Desired output dimensions.</param>
    /// <param name="options">Ordered a fallback chain of <see cref="RoiOption" /> items.</param>
    ValueTask<IImage?> CropAsync(IImage image, Size targetSize, params RoiOption[] options);
}