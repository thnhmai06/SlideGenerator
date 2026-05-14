/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfWorkbook.cs
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
using SlideGenerator.Document.Domain.Models.Sheet;
using Syncfusion.XlsIO;
using IWorksheet = SlideGenerator.Document.Domain.Abstractions.Sheet.IWorksheet;

namespace SlideGenerator.Document.Infrastructure.Adapters.Sheet;

/// <summary>
///     Wraps a Syncfusion IWorkbook and its FileStream for proper disposal and saving.
///     Uses lazy initialization to defer file access until the <see cref="Value" /> is accessed.
/// </summary>
internal sealed class SfWorkbook(
    IWorkbook value,
    BookIdentifier identifier,
    FileStream? fileStream = null) : Domain.Abstractions.Sheet.IWorkbook
{
    public IEnumerable<IWorksheet> Worksheets
    {
        get { return value.Worksheets.Select(worksheet => new SfWorksheet(worksheet)); }
    }

    public IWorksheet? GetWorksheet(string name)
    {
        var ws = value.Worksheets[name];
        return ws != null ? new SfWorksheet(ws) : null;
    }

    /// <summary>
    ///     Saves the workbook to its original location.
    /// </summary>
    public void Save()
    {
        switch (identifier.GetBookType())
        {
            case BookType.Csv:
            case BookType.Tsv:
                if (fileStream == null)
                    value.SaveAs(identifier.BookPath, identifier.Separator);
                else
                    value.SaveAs(fileStream, identifier.Separator);
                break;

            case BookType.Xls:
            case BookType.Xlsx:
            case BookType.Xltx:
            case BookType.Ods:
            default:
                if (fileStream == null)
                    value.SaveAs(identifier.BookPath);
                else
                    value.SaveAs(fileStream);
                break;
        }
    }

    /// <summary>
    ///     Closes the workbook and disposes of any underlying file streams.
    /// </summary>
    public void Dispose()
    {
        value.Close();
        fileStream?.Dispose();
    }
}
