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
///     Provides methods to analyze and summarize the structure of Excel workbooks and PowerPoint presentations.
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    ///     Specifies the maximum number of rows to include in a worksheet preview.
    /// </summary>
    /// <remarks>
    ///     This constant is used to limit the number of rows returned during preview generation
    ///     in summarization operations, ensuring that previews remain lightweight and efficient
    ///     while providing enough data for meaningful insight.
    /// </remarks>
    public const uint MaxPreviewRows = 20;

    /// <summary>
    ///     Analyzes an Excel workbook to extract sheet names, row counts, and optional data previews.
    /// </summary>
    /// <param name="identifier">The workbook identifier containing a path and optional password.</param>
    /// <param name="getPreview">Whether to include data row previews in the result.</param>
    /// <returns>A summary of the workbook structure.</returns>
    /// <param name="ct">Token to cancel the wait.</param>
    /// <exception cref="FileNotFoundException">Thrown if the workbook path is invalid.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="ct" /> is canceled while waiting.</exception>
    Task<WorkbookSummary> SummarizeWorkbookAsync(WorkbookIdentifier identifier, bool getPreview = true,
        CancellationToken ct = default);

    /// <summary>
    ///     Analyzes a PowerPoint presentation to identify slides, text placeholders, and image-compatible shapes.
    /// </summary>
    /// <param name="identifier">The presentation identifier containing a path and optional password.</param>
    /// <param name="getPreview">Whether to include slide thumbnail previews in the result.</param>
    /// <returns>A summary of the presentation structure.</returns>
    /// <param name="ct">Token to cancel the wait.</param>
    /// <exception cref="FileNotFoundException">Thrown if the presentation path is invalid.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="ct" /> is canceled while waiting.</exception>
    Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier, bool getPreview = true,
        CancellationToken ct = default);
}

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
    public async Task<WorkbookSummary> SummarizeWorkbookAsync(
        WorkbookIdentifier identifier, bool getPreview, CancellationToken ct)
    {
        if (!File.Exists(identifier.BookPath))
            throw new FileNotFoundException("Workbook not found.", identifier.BookPath);

        using var workbook = await workbookOpener.OpenWorkbookReadOnlyAsync(identifier, ct)
            .ConfigureAwait(false);
        var worksheets = new List<WorksheetSummary>();
        foreach (var worksheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();
            var headers = worksheet.GetRow(1);
            var count = worksheet.RowCount - 1;

            WorksheetPreview? preview = null;
            if (getPreview)
            {
                var rows = new List<IReadOnlyList<string>>();
                for (var i = 2; i <= Math.Min(ISummarizationService.MaxPreviewRows, count) + 1; i++)
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
    public async Task<PresentationSummary> SummarizePresentationAsync(
        PresentationIdentifier identifier, bool getPreview, CancellationToken ct)
    {
        if (!File.Exists(identifier.PresentationPath))
            throw new FileNotFoundException("Presentation not found.", identifier.PresentationPath);

        using var presentation = await presentationOpener
            .OpenPresentationReadOnlyAsync(identifier, ct).ConfigureAwait(false);

        var slides = new List<SlideSummary>();
        foreach (var pair in presentation.Slides.Select((slide, index) => new { Item = slide, Index = index }))
        {
            ct.ThrowIfCancellationRequested();

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