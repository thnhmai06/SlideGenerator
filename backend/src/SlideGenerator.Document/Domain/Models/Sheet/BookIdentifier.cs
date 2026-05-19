/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
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

namespace SlideGenerator.Document.Domain.Models.Sheet;

/// <summary>
///     Uniquely identifies an Excel workbook file.
/// </summary>
/// <param name="BookPath">The absolute or relative path to the workbook.</param>
/// <param name="BookPassword">Optional password if the workbook is encrypted.</param>
/// <param name="Separator">Optional separator for text-based formats like CSV or TSV.</param>
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

    /// <summary>
    ///     Determines the type of the workbook based on its file extension.
    /// </summary>
    /// <returns>The <see cref="BookType" /> corresponding to the file extension.</returns>
    public BookType GetBookType()
    {
        return BookTypeExtensions.FromExtension(Path.GetExtension(BookPath));
    }
}