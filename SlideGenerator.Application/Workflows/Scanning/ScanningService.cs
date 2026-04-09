using SlideGenerator.Application.Resources;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Previews;
using SlideGenerator.Domain.Workflows.Models.Scanning.Sheets;
using SlideGenerator.Domain.Workflows.Models.Scanning.Slides;

namespace SlideGenerator.Application.Workflows.Scanning;

public sealed class ScanningService(
    Registry<IPresentation> slideRegistry,
    ITextReplacer textReplacer,
    Registry<IReadOnlyWorkbook> workbookRegistry)
{
    public PresentationSummary ScanPresentation(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Presentation file path must be provided.", nameof(filePath));

        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Presentation file not found.", filePath);

        var presentation = slideRegistry.GetOrOpen(filePath, true);
        try
        {
            var slides = presentation.EnumerateSlides()
                .Select((slide, index) =>
                {
                    var shapes = slide.DescendShapes().ToList();
                    var placeholders = shapes
                        .SelectMany(textReplacer.Scan)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    var imageShapePreviews = shapes
                        .Where(shape => shape.IsPicture || shape.HasBlipFill)
                        .Select(shape =>
                    {
                        var image = shape.TryGetPicture(out var picture)
                            ? picture
                            : shape.TryGetBlipFill(out var blipFill)
                                ? blipFill
                                : [];

                        return new ShapePreview(shape.Id, shape.Name, shape.Bounds, image);
                    })
                        .ToList();

                    var slidePreview = new SlidePreview(
                        index + 1,
                        slide.Id,
                        slide.Name ?? string.Empty,
                        imageShapePreviews.FirstOrDefault()?.Image ?? []);

                    return new SlideSummary(index + 1, slidePreview, placeholders, imageShapePreviews);
                })
                .ToList();

            return new PresentationSummary(filePath, slides);
        }
        finally
        {
            slideRegistry.Close(filePath);
        }
    }

    public WorkbookSummary ScanWorkbook(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Workbook file path must be provided.", nameof(filePath));

        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Workbook file not found.", filePath);

        using var workbookLease = workbookRegistry.Acquire(filePath, true);
        var workbook = workbookLease.Value;

        var worksheets = workbook.Worksheets
            .Select(worksheet => new WorksheetSummary(worksheet.Name, worksheet.Headers, worksheet.RowsCount))
            .ToList();

        return new WorkbookSummary(workbook.FilePath, workbook.Name, worksheets);
    }
}