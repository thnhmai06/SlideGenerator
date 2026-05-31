/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: BookType.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Models.Sheet;

/// <summary>
///     Specifies the supported file types for Excel-like workbooks.
/// </summary>
public enum BookType
{
    /// <summary>Excel 97-2003 Workbook (.xls)</summary>
    Xls,

    /// <summary>Excel Workbook (.xlsx)</summary>
    Xlsx,

    /// <summary>Excel Template (.xltx)</summary>
    Xltx,

    /// <summary>OpenDocument Spreadsheet (.ods)</summary>
    Ods,

    /// <summary>Comma-Separated Values (.csv)</summary>
    Csv,

    /// <summary>Tab-Separated Values (.tsv)</summary>
    Tsv
}

/// <summary>
///     Provides extension methods and utilities for <see cref="BookType" />.
/// </summary>
public static class BookTypeExtensions
{
    /// <summary>
    ///     Gets the standard file extension associated with the specified workbook type.
    /// </summary>
    /// <param name="type">The workbook type.</param>
    /// <returns>The file extension (e.g., ".xlsx").</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the type is not recognized.</exception>
    public static string GetExtension(this BookType type)
    {
        return type switch
        {
            BookType.Xls => ".xls",
            BookType.Xlsx => ".xlsx",
            BookType.Xltx => ".xltx",
            BookType.Ods => ".ods",
            BookType.Csv => ".csv",
            BookType.Tsv => ".tsv",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    /// <summary>
    ///     Resolves the <see cref="BookType" /> from a file extension.
    /// </summary>
    /// <param name="extension">The file extension (case-insensitive).</param>
    /// <returns>The corresponding <see cref="BookType" />.</returns>
    /// <exception cref="ArgumentException">Thrown if the extension is not supported.</exception>
    public static BookType FromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".xls" => BookType.Xls,
            ".xlsx" => BookType.Xlsx,
            ".xltx" => BookType.Xltx,
            ".ods" => BookType.Ods,
            ".csv" => BookType.Csv,
            ".tsv" => BookType.Tsv,
            _ => throw new ArgumentException($"Unsupported file extension: {extension}", nameof(extension))
        };
    }
}