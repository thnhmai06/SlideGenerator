/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ColumnIdentifier.cs
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
///     Uniquely identifies a specific column within an Excel worksheet.
/// </summary>
/// <param name="BookPath">The path to the workbook.</param>
/// <param name="SheetName">The name of the worksheet.</param>
/// <param name="ColumnName">The name (header) of the column.</param>
/// <param name="BookPassword">Optional password for the workbook.</param>
public record ColumnIdentifier(string BookPath, string SheetName, string ColumnName, string? BookPassword = null)
    : SheetIdentifier(BookPath, SheetName, BookPassword);