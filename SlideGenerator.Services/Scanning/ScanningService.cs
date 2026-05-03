using SlideGenerator.Services.Scanning.Models.Sheets.Requests;
using SlideGenerator.Services.Scanning.Models.Sheets.Responses;
using SlideGenerator.Services.Scanning.Models.Slides.Requests;
using SlideGenerator.Services.Scanning.Models.Slides.Responses;
using SlideGenerator.Sheets;
using SlideGenerator.Slides.Entities;
using SlideGenerator.Slides.Services;
using Syncfusion.Presentation;
using Syncfusion.XlsIO;

namespace SlideGenerator.Services.Scanning;

public sealed class ScanningService(ExcelEngine excelEngine)
{
    private const uint MaxPreviewRows = 20;

    public Task<WorkbookSummary> ScanWorkbookAsync(BookSummaryRequest request)
    {
        var fullPath = Path.GetFullPath(request.WorkbookPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Workbook not found.", fullPath);

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var workbook = excelEngine.Excel.Workbooks.Open(stream);
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

                worksheets.Add(new WorksheetSummary(worksheet.Name, count, preview));
            }

            return Task.FromResult(
                new WorkbookSummary(fullPath, Path.GetFileNameWithoutExtension(fullPath), worksheets));
        }
        finally
        {
            workbook.Close();
        }
    }

    public static Task<PresentationSummary> ScanPresentationAsync(PresentationSummaryRequest request)
    {
        var fullPath = Path.GetFullPath(request.FilePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Presentation not found.", fullPath);

        using var wrapper = new SfPresentation(fullPath, false, request.Password);
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
                    Id: (uint)(shape.ShapeName?.GetHashCode() ?? 0),
                    Name: shape.ShapeName ?? string.Empty,
                    Bounds: Slides.Utilities.GetBoundsF(shape))
                )
                .ToList();

            slides.Add(
                new SlideSummary(
                    (uint)i + 1, (uint)i + 1, slide.Name ?? string.Empty,
                    placeholders, imageShapes, slidePreviewBytes));
        }

        return Task.FromResult(new PresentationSummary(fullPath, slides));
    }
}