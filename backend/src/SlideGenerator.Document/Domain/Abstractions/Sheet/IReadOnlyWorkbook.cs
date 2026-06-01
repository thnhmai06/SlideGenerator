/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyWorkbook.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Sheet;

/// <summary>
///     Represents a read-only view of an Excel workbook.
/// </summary>
public interface IReadOnlyWorkbook : IDisposable
{
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