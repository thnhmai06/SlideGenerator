/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiMode.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Application.Models;

/// <summary>
///     Discriminates between the two top-level ROI strategies.
/// </summary>
public enum RoiMode
{
    /// <summary>Anchor-point-based crop using geometry and optional face detection.</summary>
    Anchor,

    /// <summary>Content-aware crop using a library-specific interest strategy.</summary>
    Interest
}