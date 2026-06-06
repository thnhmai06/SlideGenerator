/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: SlideContext.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Generator.Domain.Models.Contexts;

/// <summary>
///     Represents the data required to fill a specific slide corresponding to a row in a worksheet.
///     Uses composition to hold both text and image replacement instructions.
/// </summary>
public sealed class SlideContext(SheetContext sheetContext, int rowIndex)
{
    /// <summary>Gets the parent worksheet context.</summary>
    public SheetContext SheetContext { get; } = sheetContext;

    /// <summary>Gets the 1-based row index in the worksheet that this slide represents.</summary>
    public int RowIndex { get; } = rowIndex;

    /// <summary>
    ///     Gets the text replacements where Key is the Placeholder text (Mustache tag) and Value is the string replacement.
    /// </summary>
    public Dictionary<string, string> TextReplacements { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the image replacements where Key is the ShapeIdentifier and Value is the ImageContext responsible for
    ///     processing it.
    /// </summary>
    public Dictionary<ShapeIdentifier, ImageContext> ImageReplacements { get; set; } = new();
}