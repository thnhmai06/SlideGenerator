/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiCalculator.cs
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

namespace SlideGenerator.Image.Application.Entities;

internal abstract class RoiCalculator
{
    /// <summary>
    ///     Calculates ROI by routing to the calculator keyed by the option type.
    /// </summary>
    /// <param name="image">The source image.</param>
    /// <param name="targetSize">The desired ROI size.</param>
    /// <param name="option">The customized ROI option used to resolve the target calculator.</param>
    /// <returns>The calculated ROI rectangle.</returns>
    public abstract ValueTask<Rectangle> CalculateRoiAsync(
        IImage image,
        Size targetSize,
        RoiOption option);
}