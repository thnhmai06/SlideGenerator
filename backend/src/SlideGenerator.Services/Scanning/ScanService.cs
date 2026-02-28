using SlideGenerator.Framework.Sheet.Services;
using SlideGenerator.Framework.Slide.Services;
using SlideGenerator.Services.Scanning.Models.Sheets;
using SlideGenerator.Services.Scanning.Models.Slides;

namespace SlideGenerator.Services.Scanning;

/// <remarks>
/// Reviewed by @thnhmai06 at 01/03/2026 00:36:09 GMT+7
/// </remarks>
public sealed class ScanService
{
    public static async Task<Presentation> ScanPresentationAsync(string filePath, CancellationToken cancellationToken)
    {
        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Slide template file not found.", filePath);

        using var document = XmlPresentationService.OpenOrCreatePresentation(filePath, false);

        var result = new List<Slide>();
        foreach (var slidePart in XmlPresentationService.EnumerateSlides(document))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var placeholders = TextReplacer.GetUniquePlaceholders(slidePart);
            var imageIds = ShapeService.GetImageShapeIds(slidePart);

            result.Add(new Slide(result.Count, placeholders, imageIds));
        }

        return await Task.FromResult(new Presentation(filePath, result));
    }

    public static Task<Workbook> ScanWorkbookAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Sheet data file not found.", filePath);

            using var workbook = WorkbookService.OpenWorkbook(filePath);
            var sheets = new List<Worksheet>();
            foreach (var sheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var contentRange = WorksheetService.GetContentRange(sheet);
                if (contentRange == null)
                {
                    sheets.Add(new Worksheet(sheet.Name, [], 0));
                    continue;
                }

                var headers = contentRange.FirstRow().Cells()
                    .Select(cell => cell.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();
                var recordCount = Math.Max(0, contentRange.RowCount() - 1);
                sheets.Add(new Worksheet(sheet.Name, headers, recordCount));
            }

            return Task.FromResult(new Workbook(filePath, sheets));
        }
        catch (Exception exception)
        {
            return Task.FromException<Workbook>(exception);
        }
    }
}