/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyWorksheet.cs
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
namespace SlideGenerator.Document.Domain.Abstractions.Sheet;

/// <summary>
/// Represents a read-only view of an Excel worksheet.
/// </summary>
public interface IReadOnlyWorksheet
{
    /// <summary>
    /// Gets the name of the worksheet.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the total number of rows in the worksheet.
    /// </summary>
    int RowCount { get; }

    /// <summary>
    /// Gets the total number of columns in the worksheet.
    /// </summary>
    int ColumnCount { get; }

    /// <summary>
    /// Gets the value of a cell at the specified row and column indices.
    /// </summary>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="col">The 1-based column index.</param>
    /// <returns>The string value of the cell.</returns>
    string GetCellValue(int row, int col);

    /// <summary>
    /// Gets all cell values in a specified row.
    /// </summary>
    /// <param name="rowIndex">The 1-based row index.</param>
    /// <returns>A read-only list of string values in the row.</returns>
    IReadOnlyList<string> GetRow(int rowIndex);
}






