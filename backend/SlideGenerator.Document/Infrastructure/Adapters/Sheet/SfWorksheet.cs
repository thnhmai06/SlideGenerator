/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfWorksheet.cs
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
using Syncfusion.XlsIO;

namespace SlideGenerator.Document.Infrastructure.Adapters.Sheet;

internal sealed class SfWorksheet(IWorksheet worksheet) : Domain.Abstractions.Sheet.IWorksheet
{
    public string Name => worksheet.Name;

    public int RowCount
    {
        get
        {
            var used = worksheet.UsedRange;
            return used?.LastRow - 1 ?? 0;
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

    public string GetCellValue(int row, int col)
    {
        return worksheet.Range[row, col].DisplayText ?? string.Empty;
    }

    public IReadOnlyList<string> GetRow(int rowIndex)
    {
        var used = worksheet.UsedRange;
        if (used == null || rowIndex < 0) return [];

        var cols = used.LastColumn - used.Column + 1;
        var dataRowAbsolute = used.Row + rowIndex;
        var result = new List<string>(cols);

        for (var col = 0; col < cols; col++)
        {
            var absCol = used.Column + col;
            result.Add(worksheet.Range[dataRowAbsolute, absCol]?.DisplayText ?? string.Empty);
        }

        return result;
    }

    public void SetCellValue(int row, int col, string value)
    {
        worksheet.Range[row, col].Text = value;
    }
}
