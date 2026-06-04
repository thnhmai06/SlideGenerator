/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: ISummarizationService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Summarization.Domain.Models.Sheet;
using SlideGenerator.Summarization.Domain.Models.Slide;

namespace SlideGenerator.Summarization.Application.Abstractions;

/// <summary>
///     Provides methods to analyze and summarize the structure of Excel workbooks and PowerPoint presentations.
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    ///     Analyzes an Excel workbook to extract sheet names, row counts, and optional data previews.
    /// </summary>
    /// <param name="identifier">The workbook identifier containing path and optional password.</param>
    /// <param name="getPreview">Whether to include data row previews in the result.</param>
    /// <returns>A summary of the workbook structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the workbook path is invalid.</exception>
    Task<WorkbookSummary> SummarizeWorkbookAsync(BookIdentifier identifier, bool getPreview = true);

    /// <summary>
    ///     Analyzes a PowerPoint presentation to identify slides, text placeholders, and image-compatible shapes.
    /// </summary>
    /// <param name="identifier">The presentation identifier containing a path and optional password.</param>
    /// <param name="getPreview">Whether to include slide thumbnail previews in the result.</param>
    /// <returns>A summary of the presentation structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the presentation path is invalid.</exception>
    Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier, bool getPreview = true);

}