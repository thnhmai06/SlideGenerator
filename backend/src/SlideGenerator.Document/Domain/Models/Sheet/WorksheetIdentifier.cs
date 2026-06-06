/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: WorksheetIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Models.Sheet;

/// <summary>
///     Identifies a worksheet by name within an already-known workbook context.
///     Use alongside <see cref="WorkbookIdentifier" /> when workbook context is supplied separately.
/// </summary>
/// <param name="SheetName">The name of the worksheet.</param>
public record WorksheetIdentifier(string SheetName);