/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: InterestType.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Application.Models;

/// <summary>
///     Specifies the content-aware crop strategy to use.
///     Maps to <c>NetVips.Enums.Interesting</c> (excluding <c>Centre</c>).
/// </summary>
public enum InterestType
{
    /// <summary>Maximizes the image entropy (texture / detail density).</summary>
    Entropy,

    /// <summary>Detects salient regions — faces, eyes, and other areas of visual attention.</summary>
    Attention,

    /// <summary>Crops toward the low (top-left) region of the image.</summary>
    Low,

    /// <summary>Crops toward the high (bottom-right) region of the image.</summary>
    High,

    /// <summary>Attempts to include all interesting content in the crop.</summary>
    All
}