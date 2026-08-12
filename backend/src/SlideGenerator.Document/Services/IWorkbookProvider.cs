/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IWorkbookProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Adapters.Sheet;
using SlideGenerator.Document.Models.Sheet;

namespace SlideGenerator.Document.Services;

/// <summary>
///     Defines the contract for opening Excel workbooks.
///     Hides the Syncfusion <c>ExcelEngine</c> lifecycle from callers.
/// </summary>
public interface IWorkbookProvider
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
