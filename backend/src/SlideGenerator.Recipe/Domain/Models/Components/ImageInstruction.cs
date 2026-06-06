/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: ImageInstruction.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Recipe.Domain.Models.Components;

/// <summary>
///     Defines a mapping between one or more Excel columns and one or more image shapes in a slide.
///     Includes rules for image processing and fallback behavior.
/// </summary>
/// <param name="Shapes">The set of target PowerPoint shapes to be filled with images.</param>
/// <param name="Columns">The list of Excel columns providing image URIs or paths.</param>
/// <param name="ImageEdits">The processing rules (ROI, crop, resize) for the images.</param>
/// <param name="FallbackImagePath">Optional path to a default image if the source is missing or invalid.</param>
public record ImageInstruction(
    IReadOnlySet<ShapeIdentifier> Shapes,
    IReadOnlyList<ColumnIdentifier> Columns,
    ImageEdits ImageEdits,
    string? FallbackImagePath = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the fallback image.
    /// </summary>
    public string? FallbackImagePath
    {
        get;
        init => field = string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
    } = FallbackImagePath;
}