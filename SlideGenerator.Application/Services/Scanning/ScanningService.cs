using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Application.Services.Scanning;

/// <summary>
///     Provides scanning services for presentations and workbooks to extract structure and metadata.
/// </summary>
/// <param name="slideRegistry">The registry for accessing presentation files.</param>
/// <param name="textReplacer">The replacer service used to scan text placeholders.</param>
/// <param name="workbookRegistry">The registry for accessing workbook files.</param>
public sealed class ScanningService(
    FileRegistry<IPresentation> slideRegistry,
    ITextReplacer textReplacer,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry)
{
    /// <summary>
    ///     Scans a presentation to extract its slides, placeholders, and image shapes.
    /// </summary>
    /// <param name="filePath">The file path to the presentation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PresentationSummary" /> containing the scanned metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the presentation file does not exist.</exception>
    public async Task<PresentationSummary> ScanPresentationAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Presentation file path must be provided.", nameof(filePath));

        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Presentation file not found.", filePath);

        using var lease = await slideRegistry
            .AcquireAsync(filePath, false, cancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
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

    /// <summary>
    ///     Scans a workbook to extract its worksheets, headers, and row counts.
    /// </summary>
    /// <param name="filePath">The file path to the workbook.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="WorkbookSummary" /> containing the scanned metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the workbook file does not exist.</exception>
    public async Task<WorkbookSummary> ScanWorkbookAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Workbook file path must be provided.", nameof(filePath));

        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Workbook file not found.", filePath);

        using var workbookLease = await workbookRegistry
            .AcquireAsync(filePath, false, cancellationToken)
            .ConfigureAwait(false);

        var workbook = workbookLease.Value;
        var worksheets = workbook.Worksheets
            .Select(worksheet => new WorksheetSummary(worksheet.Name, worksheet.Headers, worksheet.RowsCount))
            .ToList();

        return new WorkbookSummary(workbook.FilePath, workbook.Name, worksheets);
    }
}