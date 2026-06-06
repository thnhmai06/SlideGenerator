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

using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Models.Sheet;

namespace SlideGenerator.Document.Application.Abstractions;

/// <summary>
///     Defines the contract for opening Excel workbooks.
///     Hides the Syncfusion <c>ExcelEngine</c> lifecycle from callers.
/// </summary>
public interface IWorkbookProvider
{
    /// <summary>
    ///     Opens a workbook identified by <paramref name="identifier" /> in <b>read-write</b> mode.
    /// </summary>
    /// <param name="identifier">The workbook to open.</param>
    /// <returns>A handle wrapping the opened workbook.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the workbook file does not exist.</exception>
    IWorkbook OpenWorkbook(WorkbookIdentifier identifier);

    /// <summary>
    ///     Opens a workbook identified by <paramref name="identifier" /> in <b>read</b> mode.
    /// </summary>
    /// <param name="identifier">The workbook to open.</param>
    /// <returns>A handle wrapping the opened workbook.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the workbook file does not exist.</exception>
    IReadOnlyWorkbook OpenWorkbookReadOnly(WorkbookIdentifier identifier);
}