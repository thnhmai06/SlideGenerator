/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IWorkbookProvider.cs
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
    IWorkbook OpenWorkbook(BookIdentifier identifier);
    
    /// <summary>
    ///     Opens a workbook identified by <paramref name="identifier" /> in <b>read</b> mode.
    /// </summary>
    /// <param name="identifier">The workbook to open.</param>
    /// <returns>A handle wrapping the opened workbook.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the workbook file does not exist.</exception>
    IReadOnlyWorkbook OpenWorkbookReadOnly(BookIdentifier identifier);
}






