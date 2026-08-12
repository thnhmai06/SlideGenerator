/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ColumnIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Workbook;

/// <summary>
///     Identifies a column by its header name within an already-known worksheet context.
///     Use alongside <see cref="WorksheetIdentifier" /> and <see cref="WorkbookIdentifier" />
///     when parent context is supplied separately.
/// </summary>
/// <param name="ColumnName">The header name of the column.</param>
public record ColumnIdentifier(string ColumnName);