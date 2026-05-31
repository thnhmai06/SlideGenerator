/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiOption.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Application.Models;

/// <summary>
///     Provides customization options for ROI calculation.
/// </summary>
public abstract record RoiOption
{
    /// <summary>
    ///     Gets the region of interest (ROI) detection type.
    /// </summary>
    public abstract RoiType Type { get; }
}