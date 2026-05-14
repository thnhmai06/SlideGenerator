/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Scanning
 * File: IScanningService.cs
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

using SlideGenerator.Scanning.Domain.Models.Sheet;
using SlideGenerator.Scanning.Domain.Models.Slide;

namespace SlideGenerator.Scanning.Application.Abstractions;

/// <summary>
///     Provides methods to analyze and summarize the structure of Excel workbooks and PowerPoint presentations.
/// </summary>
public interface IScanningService
{
    /// <summary>
    ///     Analyzes an Excel workbook to extract sheet names, row counts, and optional data previews.
    /// </summary>
    /// <param name="request">The request containing workbook path and preview options.</param>
    /// <returns>A summary of the workbook structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the workbook path is invalid.</exception>
    Task<WorkbookSummary> ScanWorkbookAsync(BookSummaryRequest request);

    /// <summary>
    ///     Analyzes a PowerPoint presentation to identify slides, text placeholders, and image-compatible shapes.
    /// </summary>
    /// <param name="request">The request containing presentation path and preview options.</param>
    /// <returns>A summary of the presentation structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the presentation path is invalid.</exception>
    Task<PresentationSummary> ScanPresentationAsync(PresentationSummaryRequest request);
}