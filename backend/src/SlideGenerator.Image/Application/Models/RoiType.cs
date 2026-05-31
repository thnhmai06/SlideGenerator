/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiType.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Application.Models;

/// <summary>Identifies the algorithm used to calculate the Region of Interest (ROI).</summary>
public enum RoiType
{
    /// <summary>Aligns the ROI based on the image center or detected faces.</summary>
    Center,

    /// <summary>Aligns the ROI based on the Rule of Thirds grid points.</summary>
    RuleOfThirds
}