/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IRoiResolver.cs
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
///     Defines the contract for calculating the Region of Interest (ROI) for an image
///     to be cropped/fitted into a target shape.
/// </summary>
public interface IRoiResolver
{
    /// <summary>
    ///     Asynchronously calculates the optimal ROI rectangle for the given image.
    /// </summary>
    /// <param name="image">The source image to analyze.</param>
    /// <param name="targetSize">The target shape dimensions.</param>
    /// <param name="option">Options controlling the ROI algorithm (type, padding, etc.).</param>
    /// <returns>The calculated ROI as a <see cref="Rectangle" />.</returns>
    ValueTask<Rectangle> CalculateRoiAsync(IImage image, Size targetSize, RoiOption option);
}