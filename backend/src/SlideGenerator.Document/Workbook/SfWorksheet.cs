/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfWorksheet.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Syncfusion.XlsIO;
using SyncfusionWorksheet = Syncfusion.XlsIO.IWorksheet;

namespace SlideGenerator.Document.Workbook;

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
