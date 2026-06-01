/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfWorkbookProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Infrastructure.Adapters.Sheet;
using Syncfusion.XlsIO;
using IWorkbook = SlideGenerator.Document.Domain.Abstractions.Sheet.IWorkbook;

namespace SlideGenerator.Document.Infrastructure.Services;

/// <summary>
///     Syncfusion implementation of <see cref="IWorkbookProvider" />.
///     Wraps the singleton <see cref="ExcelEngine" /> so callers never import Syncfusion types.
/// </summary>
internal sealed class SfWorkbookProvider : IWorkbookProvider
{
    private readonly ExcelEngine _engine = new();

    /// <inheritdoc />
    public IWorkbook OpenWorkbook(BookIdentifier identifier)
    {
        Syncfusion.XlsIO.IWorkbook workbook;

        switch (identifier.GetBookType())
        {
            case BookType.Csv:
            case BookType.Tsv:
                workbook = _engine.Excel.Workbooks.Open(identifier.BookPath, identifier.Separator);
                break;

            case BookType.Xls:
            case BookType.Xlsx:
            case BookType.Xltx:
            case BookType.Ods:
            default:
                workbook = _engine.Excel.Workbooks.Open(
                    identifier.BookPath, ExcelParseOptions.Default,
                    false, identifier.BookPassword);
                break;
        }

        return new SfWorkbook(workbook, identifier);
    }

    public IReadOnlyWorkbook OpenWorkbookReadOnly(BookIdentifier identifier)
    {
        Syncfusion.XlsIO.IWorkbook workbook;
        FileStream? fileStream = null;

        switch (identifier.GetBookType())
        {
            case BookType.Csv:
            case BookType.Tsv:
                fileStream = new FileStream(
                    identifier.BookPath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                workbook = _engine.Excel.Workbooks.Open(fileStream, identifier.Separator);
                break;

            case BookType.Xls:
            case BookType.Xlsx:
            case BookType.Xltx:
            case BookType.Ods:
            default:
                workbook = _engine.Excel.Workbooks.Open(
                    identifier.BookPath, ExcelParseOptions.Default,
                    true, identifier.BookPassword);
                break;
        }

        return new SfWorkbook(workbook, identifier, fileStream);
    }
}