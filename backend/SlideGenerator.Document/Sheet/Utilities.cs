/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: Utilities.cs
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

namespace SlideGenerator.Document.Sheet;

public static class Utilities
{
    extension(IWorksheet ws)
    {
        public IReadOnlyList<string> GetHeaders()
        {
            return ws.GetRow(0);
        }

        public int CountRows()
        {
            var used = ws.UsedRange;
            return used != null ? Math.Max(0, used.LastRow - used.Row) : 0; // exclude header row
        }

        public IReadOnlyList<string> GetRow(int rowIndex)
        {
            var used = ws.UsedRange;
            if (used == null || rowIndex < 0) return [];

            var cols = used.LastColumn - used.Column + 1;
            var dataRowAbsolute = used.Row + rowIndex;
            var result = new List<string>(cols);

            for (var col = 0; col < cols; col++)
            {
                var absCol = used.Column + col;
                result.Add(ws.Range[dataRowAbsolute, absCol]?.DisplayText ?? string.Empty);
            }

            return result;
        }
    }
}