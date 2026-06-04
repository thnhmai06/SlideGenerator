/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: SummarizationHandler.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Summarization.Application.Abstractions;
using SlideGenerator.Summarization.Domain.Models.Sheet;
using SlideGenerator.Summarization.Domain.Models.Slide;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>summarization.*</c> JSON-RPC methods for inspecting Excel workbooks
///     and PowerPoint presentations before a generation job is started.
/// </summary>
public sealed class SummarizationHandler(ISummarizationService summarizationService)
{
    /// <summary>
    ///     Summarizes an Excel workbook and returns its structure, including worksheet names,
    ///     row counts, and an optional data preview.
    /// </summary>
    /// <param name="identifier">The workbook identifier containing path and optional password.</param>
    /// <param name="getPreview">Whether to include data row previews in the result.</param>
    /// <returns>A <see cref="WorkbookSummary" /> describing the workbook structure.</returns>
    public Task<WorkbookSummary> SummarizeWorkbookAsync(BookIdentifier identifier, bool getPreview)
    {
        return summarizationService.SummarizeWorkbookAsync(identifier, getPreview);
    }

    /// <summary>
    ///     Summarizes a PowerPoint presentation and returns its structure, including slide placeholders,
    ///     image shape names and bounds, and optional slide thumbnail previews.
    /// </summary>
    /// <param name="identifier">The presentation identifier containing path and optional password.</param>
    /// <param name="getPreview">Whether to include slide thumbnail previews in the result.</param>
    /// <returns>A <see cref="PresentationSummary" /> describing the presentation structure.</returns>
    public Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier, bool getPreview)
    {
        return summarizationService.SummarizePresentationAsync(identifier, getPreview);
    }
}
