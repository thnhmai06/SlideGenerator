/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyWorksheet.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Sheet;

/// <summary>
///     Represents a read-only view of an Excel worksheet.
/// </summary>
public interface IReadOnlyWorksheet
{
    /// <summary>
    ///     Gets the name of the worksheet.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the total number of rows in the worksheet.
    /// </summary>
    int RowCount { get; }

    /// <summary>
    ///     Gets the total number of columns in the worksheet.
    /// </summary>
    int ColumnCount { get; }

    /// <summary>
    ///     Gets the value of a cell at the specified row and column indices.
    /// </summary>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="col">The 1-based column index.</param>
    /// <returns>The string value of the cell.</returns>
    string GetCellValue(int row, int col);

    /// <summary>
    ///     Gets all cell values in a specified row.
    /// </summary>
    /// <param name="rowIndex">The 1-based row index.</param>
    /// <returns>A read-only list of string values in the row.</returns>
    IReadOnlyList<string> GetRow(int rowIndex);
}