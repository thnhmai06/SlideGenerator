/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: SummarizationService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Slides;
using SlideGenerator.Document.Template;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Summarization.Slide;
using SlideGenerator.Summarization.Workbook;

namespace SlideGenerator.Summarization;

/// <summary>
///     Provides discovery services for Excel workbooks and PowerPoint presentations.
///     Extracts structural metadata, identifies placeholders, and generates visual previews.
/// </summary>
internal sealed class SummarizationService(
    IWorkbookOpener workbookOpener,
    IPresentationOpener presentationOpener,
    ITemplateEngine templateEngine) : ISummarizationService
{
    /// <inheritdoc />
    public async Task<WorkbookSummary> SummarizeWorkbookAsync(WorkbookIdentifier identifier, bool getPreview = true)
    {
        if (!File.Exists(identifier.BookPath))
            throw new FileNotFoundException("Workbook not found.", identifier.BookPath);

        using var workbook = await workbookOpener.OpenWorkbookReadOnlyAsync(identifier).ConfigureAwait(false);
        var worksheets = new List<WorksheetSummary>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var headers = worksheet.GetRow(1);
            var count = worksheet.RowCount - 1;

            WorksheetPreview? preview = null;
            if (getPreview)
            {
                var rows = new List<IReadOnlyList<string>>();
                for (var i = 2; i <= Math.Min(PreviewRule.MaxPreviewRows, count) + 1; i++)
                    rows.Add(worksheet.GetRow(i));

                preview = new WorksheetPreview(headers, rows);
            }

            worksheets.Add(new WorksheetSummary(
                identifier,
                new WorksheetIdentifier(worksheet.Name),
                count, preview));
        }

        return new WorkbookSummary(identifier.BookPath, Path.GetFileNameWithoutExtension(identifier.BookPath),
            worksheets);
    }

    /// <inheritdoc />
    public async Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier,
        bool getPreview = true)
    {
        if (!File.Exists(identifier.PresentationPath))
            throw new FileNotFoundException("Presentation not found.", identifier.PresentationPath);

        using var presentation = await presentationOpener
            .OpenPresentationReadOnlyAsync(identifier).ConfigureAwait(false);

        var slides = new List<SlideSummary>();
        foreach (var pair in presentation.Slides.Select((slide, index) => new {Item = slide, Index = index}))
        {
            var slide = pair.Item;
            var index = pair.Index;
            
            var shapes = slide.Shapes.ToList();

            byte[]? slidePreviewBytes = null;
            if (getPreview) slidePreviewBytes = slide.GetPreview();

            var placeholders = shapes
                .SelectMany(s => templateEngine.ScanPlaceholders(s.DisplayText))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var slideId = new SlideIdentifier(index + 1); // 1-based
            var imageShapes = shapes
                .Where(shape => shape.ImageData != null)
                .Select(shape => new ShapeSummary(slideId, new ShapeIdentifier(shape.Name), shape.Bounds))
                .ToList();

            slides.Add(new SlideSummary(identifier, slideId, placeholders, imageShapes, slidePreviewBytes));
        }

        return new PresentationSummary(identifier.PresentationPath, slides);
    }
}