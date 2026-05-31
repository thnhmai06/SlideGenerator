/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ColumnIdentifier.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Models.Sheet;

/// <summary>
///     Uniquely identifies a specific column within an Excel worksheet.
/// </summary>
/// <param name="BookPath">The path to the workbook.</param>
/// <param name="SheetName">The name of the worksheet.</param>
/// <param name="ColumnName">The name (header) of the column.</param>
/// <param name="BookPassword">Optional password for the workbook.</param>
public record ColumnIdentifier(string BookPath, string SheetName, string ColumnName, string? BookPassword = null)
    : SheetIdentifier(BookPath, SheetName, BookPassword);