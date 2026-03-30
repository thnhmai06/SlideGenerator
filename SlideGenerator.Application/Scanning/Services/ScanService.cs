using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Application.Scanning.Models.Sheets;
using SlideGenerator.Application.Scanning.Models.Slides;

namespace SlideGenerator.Application.Scanning.Services;

/// Reviewed by @thnhmai06 at 05/03/2026
public sealed class ScanService(
    IRegistry<IPresentation> slideRegistry,
    ISlideContentOperator slideContentOperator,
    IRegistry<IReadOnlyWorkbook> workbookRegistry)
{
    public Task<PresentationInfo> ScanPresentationAsync(string filePath)
    {
        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Presentation file not found.", filePath);

        var presentation = slideRegistry.GetOrOpen(filePath, isEditable: false);
        try
        {
            var result = presentation.EnumerateSlides()
            .Select((slide, index) =>
            {
                var (placeholders, imageShapeIds) = slideContentOperator.ScanTemplateContent(slide);
                return new SlideInfo(index, placeholders.ToList(), imageShapeIds.ToList());
            })
            .ToList();

            return Task.FromResult(new PresentationInfo(filePath, result));
        }
        finally
        {
            slideRegistry.Close(filePath);
        }
    }

    public Task<WorkbookInfo> ScanWorkbookAsync(string filePath)
    {
        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Workbooks file not found.", filePath);

        var workbook = workbookRegistry.GetOrOpen(filePath);
        try
        {
            var summary = workbook.SummarySheets();
            var worksheets = new List<WorksheetInfo>(summary.Count);
            foreach (var (sheetName, recordCount) in summary)
            {
                var headers = workbook.TryGetWorksheet(sheetName, out var worksheet)
                    ? worksheet.GetHeadersName().Where(value => !string.IsNullOrWhiteSpace(value)).ToList()
                    : [];

                worksheets.Add(new WorksheetInfo(sheetName, headers, recordCount));
            }

            return Task.FromResult(new WorkbookInfo(filePath, worksheets));
        }
        finally
        {
            workbookRegistry.Close(filePath);
        }
    }
}