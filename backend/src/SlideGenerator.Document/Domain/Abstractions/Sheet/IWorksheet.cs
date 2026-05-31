/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IWorksheet.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Sheet;

/// <summary>
///     Represents an Excel worksheet that can be modified.
/// </summary>
public interface IWorksheet : IReadOnlyWorksheet
{
    /// <summary>
    ///     Sets the value of a cell at the specified row and column indices.
    /// </summary>
    /// <param name="row">The 1-based row index.</param>
    /// <param name="col">The 1-based column index.</param>
    /// <param name="value">The value to set.</param>
    void SetCellValue(int row, int col, string value);
}