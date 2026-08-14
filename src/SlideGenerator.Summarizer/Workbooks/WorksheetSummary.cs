/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarizer
 * File: WorksheetSummary.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Workbooks.Identifiers;

namespace SlideGenerator.Summarizer.Workbooks;

public record WorksheetPreview(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record WorksheetSummary(
    WorkbookIdentifier Workbook,
    WorksheetIdentifier Worksheet,
    int Count,
    WorksheetPreview? Preview = null);