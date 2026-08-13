/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Instructions.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Image.Cropping;

namespace SlideGenerator.Recipe.Mappings;

#region Text

/// <summary>
///     Defines a mapping between one or more columns and one or more text placeholders in a slide.
/// </summary>
/// <param name="Placeholders">The set of placeholder tags (e.g., "{{Name}}") to be replaced.</param>
/// <param name="Columns">The list of columns whose values will provide the replacement text.</param>
public record TextInstruction(
    IReadOnlySet<string> Placeholders,
    IReadOnlyList<ColumnIdentifier> Columns);

#endregion

#region Image

/// <summary>
///     Defines the processing rules for image transformations.
/// </summary>
/// <param name="RoiOptions">
///     Ordered fallback chain of ROI options. The resolver tries each in order and uses the
///     first that succeeds. If all anchor options fail, the image-center crop is used.
/// </param>
public sealed record ImageEditInstruction(IReadOnlyList<RoiOption> RoiOptions);

/// <summary>
///     Defines a mapping between one or more Excel columns and one or more image shapes in a slide.
///     Includes rules for image processing and fallback behavior.
/// </summary>
/// <param name="Shapes">The set of target PowerPoint shapes to be filled with images.</param>
/// <param name="Columns">The list of Excel columns providing image URIs or paths.</param>
/// <param name="ImageEditInstruction">The processing rules (ROI, crop, resize) for the images.</param>
/// <param name="FallbackImagePath">Optional path to a default image if the source is missing or invalid.</param>
public record ImageInstruction(
    IReadOnlySet<ShapeIdentifier> Shapes,
    IReadOnlyList<ColumnIdentifier> Columns,
    ImageEditInstruction ImageEditInstruction,
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

#endregion