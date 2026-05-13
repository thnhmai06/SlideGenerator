/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IWorkbook.cs
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
namespace SlideGenerator.Document.Domain.Abstractions.Sheet;

/// <summary>
/// Represents an Excel workbook that can be modified and saved.
/// </summary>
public interface IWorkbook : IReadOnlyWorkbook
{
    /// <summary>
    /// Gets the collection of worksheets in the workbook.
    /// </summary>
    new IEnumerable<IWorksheet> Worksheets { get; }

    /// <summary>
    /// Gets a worksheet by its name.
    /// </summary>
    /// <param name="name">The name of the worksheet.</param>
    /// <returns>The worksheet if found; otherwise, null.</returns>
    new IWorksheet? GetWorksheet(string name);

    /// <summary>
    /// Saves the changes made to the workbook.
    /// </summary>
    void Save();

    /// <inheritdoc />
    IEnumerable<IReadOnlyWorksheet> IReadOnlyWorkbook.Worksheets => Worksheets;

    /// <inheritdoc />
    IReadOnlyWorksheet? IReadOnlyWorkbook.GetWorksheet(string name)
    {
        return GetWorksheet(name);
    }
}






