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
    ///     Gets the number of rows in the worksheet's used range, counted from its first row (the header row when
    ///     the data does not start at row 1). 0 for an empty worksheet.
    /// </summary>
    int RowCount { get; }

    /// <summary>
    ///     Gets the number of columns in the used range, counted from its first column. 0 for an empty worksheet.
    /// </summary>
    int ColumnCount { get; }

    /// <summary>
    ///     Gets the value of a cell at the specified row and column indices.
    /// </summary>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="col">The 1-based column index.</param>
    /// <returns>The string value of the cell.</returns>
    /// <remarks>Worksheet coordinates (not relative to the used range).</remarks>
    string GetCell(int row, int col);

    /// <summary>
    ///     Gets all cell values in a specified row.
    /// </summary>
    /// <param name="rowIndex">
    ///     The 1-based row index relative to the first row of the used range (row 1 = the header row).
    /// </param>
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
            if (!HasData(used)) return 0;
            return used!.LastRow - used.Row + 1;
        }
    }

    public int ColumnCount
    {
        get
        {
            var used = worksheet.UsedRange;
            if (!HasData(used)) return 0;
            return used!.LastColumn - used.Column + 1;
        }
    }

    public string GetCell(int row, int col)
    {
        return worksheet.Range[row, col].DisplayText ?? string.Empty;
    }

    public IReadOnlyList<string> GetRow(int rowIndex)
    {
        var used = worksheet.UsedRange;
        if (!HasData(used) || rowIndex < 1) return [];

        var cols = used!.LastColumn - used.Column + 1;
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

    /// <summary>
    ///     Whether the used range holds data. Syncfusion returns a 0-row/0-column range for an untouched sheet and a
    ///     1×1 range with empty text for a sheet whose only cell was written then cleared; both read as empty because
    ///     a single empty cell is indistinguishable from a sheet with no data.
    /// </summary>
    /// <param name="used">The worksheet's used range, or null when Syncfusion reports none.</param>
    /// <returns><c>true</c> when the range contains at least one meaningful cell; otherwise <c>false</c>.</returns>
    private static bool HasData(Syncfusion.XlsIO.IRange? used)
    {
        return used is not null
               && used.LastRow >= used.Row
               && used.LastColumn >= used.Column
               && (used.LastRow > used.Row
                   || used.LastColumn > used.Column
                   || !string.IsNullOrEmpty(used.DisplayText));
    }
}