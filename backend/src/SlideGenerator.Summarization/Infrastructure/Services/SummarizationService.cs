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

using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Summarization.Application.Abstractions;
using SlideGenerator.Summarization.Domain.Models.Recipes;
using SlideGenerator.Summarization.Domain.Models.Sheet;
using SlideGenerator.Summarization.Domain.Models.Slide;
using SlideGenerator.Summarization.Domain.Rules;

namespace SlideGenerator.Summarization.Infrastructure.Services;

/// <summary>
///     Provides discovery services for Excel workbooks and PowerPoint presentations.
///     Extracts structural metadata, identifies placeholders, and generates visual previews.
/// </summary>
internal sealed class SummarizationService(
    IWorkbookProvider workbookProvider,
    IPresentationProvider presentationProvider,
    ITemplateEngine templateEngine) : ISummarizationService
{
    /// <inheritdoc />
    public Task<WorkbookSummary> SummarizeWorkbookAsync(BookIdentifier identifier, bool getPreview = true)
    {
        if (!File.Exists(identifier.BookPath))
            throw new FileNotFoundException("Workbook not found.", identifier.BookPath);

        using var workbook = workbookProvider.OpenWorkbookReadOnly(identifier);
        var worksheets = new List<WorksheetSummary>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var headers = worksheet.GetRow(0);
            var count = worksheet.RowCount;

            WorksheetPreview? preview = null;
            if (getPreview)
            {
                var rows = new List<IReadOnlyList<string>>();
                for (var i = 1; i <= Math.Min(PreviewRule.MaxPreviewRows, count); i++)
                    rows.Add(worksheet.GetRow(i));

                preview = new WorksheetPreview(headers, rows);
            }

            worksheets.Add(new WorksheetSummary(
                new SheetIdentifier(identifier.BookPath, worksheet.Name, identifier.BookPassword),
                count, preview));
        }

        return Task.FromResult(
            new WorkbookSummary(identifier.BookPath, Path.GetFileNameWithoutExtension(identifier.BookPath),
                worksheets));
    }

    /// <inheritdoc />
    public Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier,
        bool getPreview = true)
    {
        if (!File.Exists(identifier.PresentationPath))
            throw new FileNotFoundException("Presentation not found.", identifier.PresentationPath);

        using var presentation = presentationProvider.OpenPresentationReadOnly(identifier);

        var slides = new List<SlideSummary>();
        foreach (var slide in presentation.Slides)
        {
            var shapes = slide.Shapes.ToList();

            byte[]? slidePreviewBytes = null;
            if (getPreview) slidePreviewBytes = slide.GetPreview();

            var placeholders = shapes
                .SelectMany(s => templateEngine.ScanPlaceholders(s.DisplayText))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var imageShapes = shapes
                .Where(shape => shape.ImageData != null)
                .Select(shape => new ShapeSummary(
                    new ShapeIdentifier(identifier.PresentationPath, slide.Number, shape.Name,
                        identifier.PresentationPassword),
                    shape.Bounds))
                .ToList();

            slides.Add(new SlideSummary(
                new SlideIdentifier(identifier.PresentationPath, slide.Number, identifier.PresentationPassword),
                placeholders, imageShapes, slidePreviewBytes));
        }

        return Task.FromResult(new PresentationSummary(identifier.PresentationPath, slides));
    }

    /// <inheritdoc />
    public RecipeSummary SummarizeRecipe(string recipe)
    {
        throw new NotImplementedException("Recipe JSON → RecipeSummary not yet implemented.");
    }
}