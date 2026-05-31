/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: BookIdentifier.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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