/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiType.cs
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

namespace SlideGenerator.Image.Application.Models;

/// <summary>Identifies the algorithm used to calculate the Region of Interest (ROI).</summary>
public enum RoiType
{
    /// <summary>Aligns the ROI based on the image center or detected faces.</summary>
    Center,

    /// <summary>Aligns the ROI based on the Rule of Thirds grid points.</summary>
    RuleOfThirds
}