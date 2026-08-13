/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: Worksheet.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Workbooks.Identifiers;
using SyncfusionWorksheet = Syncfusion.XlsIO.IWorksheet;

namespace SlideGenerator.Document.Workbooks.Components;

/// <summary>
///     Represents a read-only view of an Excel worksheet.
/// </summary>
public interface IReadOnlyWorksheet
{
    /// <summary>
    ///     Gets the identifier of the worksheet.
    /// </summary>
    WorksheetIdentifier Identifier { get; }

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
    string GetCell(int row, int col);

    /// <summary>
    ///     Gets all cell values in a specified row.
    /// </summary>
    /// <param name="rowIndex">The 1-based row index.</param>
    /// <returns>A read-only list of string values in the row.</returns>
    IReadOnlyList<string> GetRow(int rowIndex);
}

/// <summary>
///     Represents an Excel worksheet that can be modified.
/// </summary>
public interface IWorksheet : IReadOnlyWorksheet
{
    /// <summary>
    ///     Sets the value of a cell at the specified row and column indices.
    /// </summary>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="col">The 1-based column index.</param>
    /// <param name="value">The value to set.</param>
    void SetCell(int row, int col, string value);
}

internal sealed class SfWorksheet(SyncfusionWorksheet worksheet) : IWorksheet
{
    public WorksheetIdentifier Identifier => new(Name);
    public string Name => worksheet.Name;

    public int RowCount
    {
        get
        {
            var used = worksheet.UsedRange;
            return used?.LastRow ?? 0;
        }
    }

    public int ColumnCount
    {
        get
        {
            var used = worksheet.UsedRange;
            return used?.LastColumn ?? 0;
        }
    }

    public string GetCell(int row, int col)
    {
        return worksheet.Range[row, col].DisplayText ?? string.Empty;
    }

    public IReadOnlyList<string> GetRow(int rowIndex)
    {
        var used = worksheet.UsedRange;
        if (used == null || rowIndex < 1) return [];

        var cols = used.LastColumn - used.Column + 1;
        var dataRowAbsolute = used.Row + rowIndex - 1;
        var result = new List<string>(cols);

        for (var col = 0; col < cols; col++)
        {
            var absCol = used.Column + col;
            result.Add(worksheet.Range[dataRowAbsolute, absCol]?.DisplayText ?? string.Empty);
        }

        return result;
    }

    public void SetCell(int row, int col, string value)
    {
        worksheet.Range[row, col].Text = value;
    }
}
