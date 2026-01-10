namespace SlideGenerator.Application.Features.Sheets.DTOs.Requests.Workbook;

/// <summary>
///     Request to close a workbook file.
/// </summary>
public sealed record SheetWorkbookClose(string FilePath);