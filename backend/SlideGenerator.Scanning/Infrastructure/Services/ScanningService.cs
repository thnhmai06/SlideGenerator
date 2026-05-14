/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Scanning
 * File: ScanningService.cs
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

using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Scanning.Application.Abstractions;
using SlideGenerator.Scanning.Domain.Models.Sheet;
using SlideGenerator.Scanning.Domain.Models.Slide;

namespace SlideGenerator.Scanning.Infrastructure.Services;

/// <summary>
///     Provides discovery services for Excel workbooks and PowerPoint presentations.
///     Extracts structural metadata, identifies placeholders, and generates visual previews.
/// </summary>
internal sealed class ScanningService(
    IWorkbookProvider workbookProvider,
    IPresentationProvider presentationProvider,
    ITemplateEngine templateEngine) : IScanningService
{
    private const uint MaxPreviewRows = 20;

    /// <summary>
    ///     Analyzes an Excel workbook to extract sheet names, row counts, and optional data previews.
    /// </summary>
    /// <param name="request">The request containing workbook path and preview options.</param>
    /// <returns>A summary of the workbook structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the workbook path is invalid.</exception>
    public Task<WorkbookSummary> ScanWorkbookAsync(BookSummaryRequest request)
    {
        var id = request.Identifier;
        if (!File.Exists(id.BookPath))
            throw new FileNotFoundException("Workbook not found.", id.BookPath);

        using var workbook = workbookProvider.OpenWorkbookReadOnly(id);
        var worksheets = new List<WorksheetSummary>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var headers = worksheet.GetRow(0);
            var count = worksheet.RowCount;

            WorksheetPreview? preview = null;
            if (request.GetPreview)
            {
                var rows = new List<IReadOnlyList<string>>();
                for (var i = 1; i <= Math.Min(MaxPreviewRows, count); i++)
                    rows.Add(worksheet.GetRow(i));

                preview = new WorksheetPreview(headers, rows);
            }

            worksheets.Add(new WorksheetSummary(new SheetIdentifier(id.BookPath, worksheet.Name, id.BookPassword),
                count, preview));
        }

        return Task.FromResult(
            new WorkbookSummary(id.BookPath, Path.GetFileNameWithoutExtension(id.BookPath), worksheets));
    }

    /// <summary>
    ///     Analyzes a PowerPoint presentation to identify slides, text placeholders, and image-compatible shapes.
    /// </summary>
    /// <param name="request">The request containing presentation path and preview options.</param>
    /// <returns>A summary of the presentation structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the presentation path is invalid.</exception>
    public Task<PresentationSummary> ScanPresentationAsync(PresentationSummaryRequest request)
    {
        var id = request.Identifier;
        if (!File.Exists(id.PresentationPath))
            throw new FileNotFoundException("Presentation not found.", id.PresentationPath);

        using var presentation = presentationProvider.OpenPresentationReadOnly(id);

        var slides = new List<SlideSummary>();
        foreach (var slide in presentation.Slides)
        {
            var shapes = slide.Shapes.ToList();

            // Slide Preview
            byte[]? slidePreviewBytes = null;
            if (request.GetPreview) slidePreviewBytes = slide.GetPreview();

            // DisplayText Placeholders
            var placeholders = shapes
                .SelectMany(s => templateEngine.ScanPlaceholders(s.DisplayText))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // Image Shapes
            var imageShapes = shapes
                .Where(shape => shape.ImageData != null)
                .Select(shape => new ShapeSummary(
                    new ShapeIdentifier(id.PresentationPath, slide.Number, shape.Name,
                        id.PresentationPassword),
                    shape.Bounds)
                )
                .ToList();

            slides.Add(
                new SlideSummary(
                    new SlideIdentifier(id.PresentationPath, slide.Number, id.PresentationPassword),
                    placeholders, imageShapes, slidePreviewBytes));
        }

        return Task.FromResult(new PresentationSummary(id.PresentationPath, slides));
    }
}