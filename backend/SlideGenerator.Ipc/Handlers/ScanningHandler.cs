/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: ScanningHandler.cs
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
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Scanning.Application.Abstractions;
using SlideGenerator.Scanning.Domain.Models.Sheet;
using SlideGenerator.Scanning.Domain.Models.Slide;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>scanning.*</c> JSON-RPC methods for inspecting Excel workbooks
///     and PowerPoint presentations before a generation job is started.
/// </summary>
public sealed class ScanningHandler(IScanningService scanningService)
{
    /// <summary>
    ///     Scans an Excel workbook and returns its structure, including worksheet names,
    ///     row counts, and an optional data preview.
    /// </summary>
    /// <param name="request">
    ///     The scan request deserialized directly from the JSON-RPC payload,
    ///     containing a <see cref="BookIdentifier" /> and a preview flag.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    ///     A <see cref="WorkbookSummary" /> describing the workbook structure.
    /// </returns>
    public Task<WorkbookSummary> ScanWorkbookAsync(BookSummaryRequest request, CancellationToken ct)
    {
        return scanningService.ScanWorkbookAsync(request);
    }

    /// <summary>
    ///     Scans a PowerPoint presentation and returns its structure, including slide placeholders,
    ///     image shape names and bounds, and optional slide thumbnail previews.
    /// </summary>
    /// <param name="request">
    ///     The scan request deserialized directly from the JSON-RPC payload,
    ///     containing a <see cref="PresentationIdentifier" /> and a preview flag.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    ///     A <see cref="PresentationSummary" /> describing the presentation structure.
    /// </returns>
    public Task<PresentationSummary> ScanPresentationAsync(PresentationSummaryRequest request, CancellationToken ct)
    {
        return scanningService.ScanPresentationAsync(request);
    }
}
