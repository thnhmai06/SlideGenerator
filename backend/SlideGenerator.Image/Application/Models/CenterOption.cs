/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: CenterOption.cs
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
using System.Numerics;

namespace SlideGenerator.Image.Application.Models;

/// <summary>
///     Customizes the center ROI calculation.
/// </summary>
public sealed record CenterOption : RoiOption
{
    public override RoiType Type => RoiType.Center;

    /// <summary>
    ///     Gets or sets the target pivot point (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    ///     Determines where the anchor point (e.g., face, grid point) is positioned within the resulting ROI.
    /// </remarks>
    public Vector2 Pivot { get; init; } = new(1 / 2f, 1 / 2f);

    /// <summary>
    ///     Gets or sets whether to use detected faces to align the center.
    /// </summary>
    public bool UseFaceAlignment { get; init; }
}





