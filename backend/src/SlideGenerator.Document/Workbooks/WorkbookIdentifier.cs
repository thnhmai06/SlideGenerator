/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: WorkbookIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Workbooks;

/// <summary>
///     Specifies the supported file types for Excel-like workbooks.
/// </summary>
public enum WorkbookType : byte
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
///     Provides extension methods and utilities for <see cref="WorkbookType" />.
/// </summary>
public static class BookTypeExtensions
{
    /// <summary>
    ///     Gets the standard file extension associated with the specified workbook type.
    /// </summary>
    /// <param name="type">The workbook type.</param>
    /// <returns>The file extension (e.g., ".xlsx").</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the type is not recognized.</exception>
    public static string ToExtension(this WorkbookType type)
    {
        return type switch
        {
            WorkbookType.Xls => ".xls",
            WorkbookType.Xlsx => ".xlsx",
            WorkbookType.Xltx => ".xltx",
            WorkbookType.Ods => ".ods",
            WorkbookType.Csv => ".csv",
            WorkbookType.Tsv => ".tsv",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    /// <summary>
    ///     Resolves the <see cref="WorkbookType" /> from a file extension.
    /// </summary>
    /// <param name="extension">The file extension (case-insensitive).</param>
    /// <returns>The corresponding <see cref="WorkbookType" />.</returns>
    /// <exception cref="ArgumentException">Thrown if the extension is not supported.</exception>
    public static WorkbookType FromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".xls" => WorkbookType.Xls,
            ".xlsx" => WorkbookType.Xlsx,
            ".xltx" => WorkbookType.Xltx,
            ".ods" => WorkbookType.Ods,
            ".csv" => WorkbookType.Csv,
            ".tsv" => WorkbookType.Tsv,
            _ => throw new ArgumentException($"Unsupported file extension: {extension}", nameof(extension))
        };
    }
}

/// <summary>
///     Uniquely identifies an Excel workbook file.
/// </summary>
/// <param name="BookPath">The absolute or relative path to the workbook.</param>
/// <param name="BookPassword">Optional password if the workbook is encrypted.</param>
/// <param name="Separator">Optional separator for text-based formats like CSV or TSV.</param>
public record WorkbookIdentifier(string BookPath, string? BookPassword = null, string? Separator = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the workbook.
    /// </summary>
    public string BookPath
    {
        get;
        init => field = Path.IsPathRooted(value) ? Path.GetFullPath(value) : value;
    } = BookPath;

    /// <summary>
    ///     Determines the type of the workbook based on its file extension.
    /// </summary>
    /// <returns>The <see cref="WorkbookType" /> corresponding to the file extension.</returns>
    public WorkbookType GetBookType()
    {
        return BookTypeExtensions.FromExtension(Path.GetExtension(BookPath));
    }
}
