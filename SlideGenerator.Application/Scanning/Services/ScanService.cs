using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Application.Scanning.Models.Sheets;
using SlideGenerator.Application.Scanning.Models.Slides;
using SlideGenerator.Framework.Sheet.Services;
using SlideGenerator.Framework.Slide.Services;
using SlideGenerator.Framework.Slide.Services.Presentation;
using SlideGenerator.Framework.Slide.Services.Replacer;

namespace SlideGenerator.Application.Scanning.Services;

/// Reviewed by @thnhmai06 at 05/03/2026
public sealed class ScanService
{
    public static async Task<PresentationInfo> ScanPresentationAsync(string filePath)
    {
        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Presentation file not found.", filePath);

        using var document = PresentationDocument.Open(filePath, false);

        var result = new List<SlideInfo>();
        foreach (var slidePart in document.EnumerateSlides())
        {
            var mustaches = slidePart.ScanMustache()
                .Select(m => m.Mustache)
                .Distinct()
                .ToList();
            var imageIds = slidePart.GetImageShapeIds().ToList();

            result.Add(new SlideInfo(result.Count, mustaches, imageIds));
        }

        return await Task.FromResult(new PresentationInfo(filePath, result));
    }

    public static Task<WorkbookInfo> ScanWorkbookAsync(string filePath)
    {
        try
        {
            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Workbook file not found.", filePath);

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var workbook = new XLWorkbook(fs);
            var sheets = new List<WorksheetInfo>();
            foreach (var sheet in workbook.Worksheets)
            {
                var contentRange = sheet.GetContentRange();
                if (contentRange == null)
                {
                    sheets.Add(new WorksheetInfo(sheet.Name, [], 0));
                    continue;
                }

                var headers = contentRange.FirstRow().Cells()
                    .Select(cell => cell.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();
                var recordCount = Math.Max(0, contentRange.RowCount() - 1);
                sheets.Add(new WorksheetInfo(sheet.Name, headers, recordCount));
            }

            return Task.FromResult(new WorkbookInfo(filePath, sheets));
        }
        catch (Exception exception)
        {
            return Task.FromException<WorkbookInfo>(exception);
        }
    }
}