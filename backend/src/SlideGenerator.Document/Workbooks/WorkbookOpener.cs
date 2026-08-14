/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: WorkbookOpener.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Workbooks.Components;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Utilities;
using Syncfusion.XlsIO;
using IWorkbook = SlideGenerator.Document.Workbooks.Components.IWorkbook;

namespace SlideGenerator.Document.Workbooks;

/// <summary>
///     Defines the contract for opening Excel workbooks.
///     Hides the Syncfusion <c>ExcelEngine</c> lifecycle from callers.
/// </summary>
public interface IWorkbookOpener
{
    /// <summary>
    ///     Opens a workbook in <b>read-write</b> mode asynchronously.
    ///     If the file is locked by another process, waits for the lock to release via
    ///     <see cref="System.IO.FileSystemWatcher" /> before retrying.
    /// </summary>
    /// <param name="identifier">The workbook to open.</param>
    /// <param name="ct">Token to cancel the wait.</param>
    /// <returns>A handle wrapping the opened workbook.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the workbook file does not exist.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="ct" /> is canceled while waiting.</exception>
    Task<IWorkbook> OpenWorkbookAsync(WorkbookIdentifier identifier, CancellationToken ct = default);

    /// <summary>
    ///     Opens a workbook in <b>read</b> mode asynchronously.
    ///     If the file is locked by another process, waits for the lock to release via
    ///     <see cref="System.IO.FileSystemWatcher" /> before retrying.
    /// </summary>
    /// <param name="identifier">The workbook to open.</param>
    /// <param name="ct">Token to cancel the wait.</param>
    /// <returns>A handle wrapping the opened workbook.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the workbook file does not exist.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="ct" /> is canceled while waiting.</exception>
    Task<IReadOnlyWorkbook> OpenWorkbookReadOnlyAsync(WorkbookIdentifier identifier, CancellationToken ct = default);
}

/// <summary>
///     Syncfusion implementation of <see cref="IWorkbookOpener" />.
///     Wraps the singleton <see cref="ExcelEngine" /> so callers never import Syncfusion types.
/// </summary>
internal sealed class SfWorkbookOpener : IWorkbookOpener
{
    private readonly ExcelEngine _engine = new();

    /// <inheritdoc />
    public async Task<IWorkbook> OpenWorkbookAsync(WorkbookIdentifier identifier, CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return CreateWorkbookInstance(identifier);
            }
            catch (IOException ex) when (FileAccessHelper.IsFileLockedException(ex))
            {
                _ = ex;
            }

            await FileAccessHelper.WaitForFileChangeAsync(identifier.BookPath, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyWorkbook> OpenWorkbookReadOnlyAsync(WorkbookIdentifier identifier,
        CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return CreateWorkbookReadOnlyInstance(identifier);
            }
            catch (IOException ex) when (FileAccessHelper.IsFileLockedException(ex))
            {
                _ = ex;
            }

            await FileAccessHelper.WaitForFileChangeAsync(identifier.BookPath, ct).ConfigureAwait(false);
        }
    }

    private SfWorkbook CreateWorkbookInstance(WorkbookIdentifier identifier)
    {
        Syncfusion.XlsIO.IWorkbook workbook;

        switch (identifier.GetBookType())
        {
            case WorkbookType.Csv:
            case WorkbookType.Tsv:
                workbook = _engine.Excel.Workbooks.Open(identifier.BookPath, identifier.Separator);
                break;

            case WorkbookType.Xls:
            case WorkbookType.Xlsx:
            case WorkbookType.Xltx:
            case WorkbookType.Ods:
            default:
                workbook = _engine.Excel.Workbooks.Open(
                    identifier.BookPath, ExcelParseOptions.Default,
                    false, identifier.BookPassword);
                break;
        }

        return new SfWorkbook(workbook, identifier);
    }

    private SfWorkbook CreateWorkbookReadOnlyInstance(WorkbookIdentifier identifier)
    {
        Syncfusion.XlsIO.IWorkbook workbook;
        FileStream? fileStream = null;

        switch (identifier.GetBookType())
        {
            case WorkbookType.Csv:
            case WorkbookType.Tsv:
                fileStream = new FileStream(
                    identifier.BookPath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                workbook = _engine.Excel.Workbooks.Open(fileStream, identifier.Separator);
                break;

            case WorkbookType.Xls:
            case WorkbookType.Xlsx:
            case WorkbookType.Xltx:
            case WorkbookType.Ods:
            default:
                workbook = _engine.Excel.Workbooks.Open(
                    identifier.BookPath, ExcelParseOptions.Default,
                    true, identifier.BookPassword);
                break;
        }

        return new SfWorkbook(workbook, identifier, fileStream);
    }
}