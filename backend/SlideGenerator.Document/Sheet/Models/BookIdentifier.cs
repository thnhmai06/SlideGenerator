/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: BookIdentifier.cs
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

namespace SlideGenerator.Document.Sheet.Models;

/// <summary>
///     Uniquely identifies an Excel workbook file.
/// </summary>
/// <param name="BookPath">The absolute or relative path to the workbook.</param>
/// <param name="BookPassword">Optional password if the workbook is encrypted.</param>
public record BookIdentifier(string BookPath, string? BookPassword = null, string? Separator = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the workbook.
    /// </summary>
    public string BookPath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = BookPath;

    public BookType GetBookType()
    {
        return BookTypeExtensions.FromExtension(Path.GetExtension(BookPath));
    }
}