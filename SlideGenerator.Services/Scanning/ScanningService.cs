using SlideGenerator.Services.Scanning.Models.Sheets.Requests;
using SlideGenerator.Services.Scanning.Models.Sheets.Responses;
using SlideGenerator.Services.Scanning.Models.Slides.Requests;
using SlideGenerator.Services.Scanning.Models.Slides.Responses;
using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Sheets;
using SlideGenerator.Slides.Entities;
using SlideGenerator.Slides.Services;
using Syncfusion.Presentation;
using Syncfusion.XlsIO;

namespace SlideGenerator.Services.Scanning;

/// <summary>
///     Provides discovery services for Excel workbooks and PowerPoint presentations.
///     Extracts structural metadata, identifies placeholders, and generates visual previews.
/// </summary>
/// <param name="excelEngine">The Syncfusion Excel engine instance used for workbook operations.</param>
public sealed class ScanningService(ExcelEngine excelEngine)
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

        var workbook = excelEngine.Excel.Workbooks.Open(id.BookPath, ExcelParseOptions.Default, true, id.BookPassword);
        try
        {
            var worksheets = new List<WorksheetSummary>(workbook.Worksheets.Count);
            foreach (var worksheet in workbook.Worksheets)
            {
                var headers = worksheet.GetHeaders();
                var count = worksheet.CountRows();

                WorksheetPreview? preview = null;
                if (request.GetPreview)
                {
                    var rows = new List<IReadOnlyList<string>>();
                    for (var i = 1; i <= Math.Min(MaxPreviewRows, count); i++)
                        rows.Add(worksheet.GetRow(i));

                    preview = new WorksheetPreview(headers, rows);
                }

                worksheets.Add(new WorksheetSummary(new SheetIdentifier(id.BookPath, worksheet.Name, id.BookPassword), count, preview));
            }

            return Task.FromResult(
                new WorkbookSummary(id.BookPath, Path.GetFileNameWithoutExtension(id.BookPath), worksheets));
        }
        finally
        {
            workbook.Close();
        }
    }

    /// <summary>
    ///     Analyzes a PowerPoint presentation to identify slides, text placeholders, and image-compatible shapes.
    /// </summary>
    /// <param name="request">The request containing presentation path and preview options.</param>
    /// <returns>A summary of the presentation structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the presentation path is invalid.</exception>
    public static Task<PresentationSummary> ScanPresentationAsync(PresentationSummaryRequest request)
    {
        var id = request.Identifier;
        if (!File.Exists(id.PresentationPath))
            throw new FileNotFoundException("Presentation not found.", id.PresentationPath);

        using var wrapper = new SfPresentation(id.PresentationPath, false, id.PresentationPassword);
        var presentation = wrapper.Value;

        var slides = new List<SlideSummary>();
        for (var i = 0; i < presentation.Slides.Count; i++)
        {
            var slide = presentation.Slides[i];
            var shapes = slide.Shapes.Cast<Syncfusion.Presentation.IShape>().ToList();

            // Slide Preview
            byte[]? slidePreviewBytes = null;
            if (request.GetPreview) slidePreviewBytes = Slides.Utilities.GetPreview(slide);

            // Text Placeholders
            var placeholders = shapes
                .SelectMany(TextComposer.Scan)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // Image Shapes
            var imageShapes = shapes
                .Where(shape => shape is IPicture || shape.Fill.FillType == FillType.Picture)
                .Select(shape => new ShapeSummary(
                    new ShapeIdentifier(id.PresentationPath, i + 1, shape.ShapeName ?? string.Empty, id.PresentationPassword),
                    Slides.Utilities.GetBoundsF(shape))
                )
                .ToList();

            slides.Add(
                new SlideSummary(
                    new SlideIdentifier(id.PresentationPath, i + 1, id.PresentationPassword),
                    placeholders, imageShapes, slidePreviewBytes));
        }

        return Task.FromResult(new PresentationSummary(id.PresentationPath, slides));
    }
}