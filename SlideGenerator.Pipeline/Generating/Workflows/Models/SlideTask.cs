/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
 * File: SlideTask.cs
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

using SlideGenerator.Document.Slide.Models;

namespace SlideGenerator.Pipeline.Generating.Workflows.Models;

/// <summary>
///     Represents the data required to fill a specific slide corresponding to a row in a sheet.
///     Uses composition to hold both text and image replacement instructions.
/// </summary>
public sealed class SlideTask(SheetTask sheetTask, int rowIndex)
{
    /// <summary>Gets the parent worksheet task.</summary>
    public SheetTask SheetTask { get; } = sheetTask;

    /// <summary>Gets the 1-based row index in the sheet that this slide represents.</summary>
    public int RowIndex { get; } = rowIndex;

    /// <summary>
    ///     Gets the text replacements where Key is the Placeholder text (Mustache tag) and Value is the string replacement.
    /// </summary>
    public Dictionary<string, string> TextReplacements { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the image replacements where Key is the ShapeIdentifier and Value is the ImageTask responsible for processing
    ///     it.
    /// </summary>
    public Dictionary<ShapeIdentifier, ImageTask> ImageReplacements { get; } = new();
}