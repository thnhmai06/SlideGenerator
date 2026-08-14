/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: Workbook.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Workbooks.Identifiers;
using SyncfusionWorkbook = Syncfusion.XlsIO.IWorkbook;

namespace SlideGenerator.Document.Workbooks.Components;

/// <summary>
///     Represents a read-only view of an Excel workbook.
/// </summary>
public interface IReadOnlyWorkbook : IDisposable
{
    /// <summary>
    ///     Gets the identifier of the workbook.
    /// </summary>
    WorkbookIdentifier Identifier { get; }

    /// <summary>
    ///     Gets the collection of worksheets in the workbook.
    /// </summary>
    IEnumerable<IReadOnlyWorksheet> Worksheets { get; }

    /// <summary>
    ///     Gets a worksheet by its name.
    /// </summary>
    /// <param name="name">The name of the worksheet.</param>
    /// <returns>The worksheet if found; otherwise, null.</returns>
    IReadOnlyWorksheet? GetWorksheet(string name);
}

/// <summary>
///     Represents an Excel workbook that can be modified and saved.
/// </summary>
public interface IWorkbook : IReadOnlyWorkbook
{
    /// <summary>
    ///     Gets the collection of worksheets in the workbook.
    /// </summary>
    new IEnumerable<IWorksheet> Worksheets { get; }

    /// <inheritdoc />
    IEnumerable<IReadOnlyWorksheet> IReadOnlyWorkbook.Worksheets => Worksheets;

    /// <inheritdoc />
    IReadOnlyWorksheet? IReadOnlyWorkbook.GetWorksheet(string name)
    {
        return GetWorksheet(name);
    }

    /// <summary>
    ///     Gets a worksheet by its name.
    /// </summary>
    /// <param name="name">The name of the worksheet.</param>
    /// <returns>The worksheet if found; otherwise, null.</returns>
    new IWorksheet? GetWorksheet(string name);

    /// <summary>
    ///     Saves the changes made to the workbook.
    /// </summary>
    void Save();
}

/// <summary>
///     Wraps a Syncfusion IWorkbook and its FileStream for proper disposal and saving.
/// </summary>
internal sealed class SfWorkbook(
    SyncfusionWorkbook value,
    WorkbookIdentifier identifier,
    FileStream? fileStream = null) : IWorkbook
{
    public WorkbookIdentifier Identifier { get; } = identifier;

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
        switch (Identifier.GetBookType())
        {
            case WorkbookType.Csv:
            case WorkbookType.Tsv:
                if (fileStream == null)
                    value.SaveAs(Identifier.BookPath, Identifier.Separator);
                else
                    value.SaveAs(fileStream, Identifier.Separator);
                break;

            case WorkbookType.Xls:
            case WorkbookType.Xlsx:
            case WorkbookType.Xltx:
            case WorkbookType.Ods:
            default:
                if (fileStream == null)
                    value.SaveAs(Identifier.BookPath);
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