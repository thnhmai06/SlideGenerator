/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: CenterOption.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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