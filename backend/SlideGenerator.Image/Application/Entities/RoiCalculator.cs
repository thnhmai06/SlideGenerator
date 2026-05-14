/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiCalculator.cs
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
    public abstract ValueTask<Rectangle> CalculateRoiAsync(IImage image, Size targetSize,
        RoiOption option);
}
