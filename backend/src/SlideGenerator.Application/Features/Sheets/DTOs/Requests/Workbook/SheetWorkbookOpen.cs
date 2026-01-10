namespace SlideGenerator.Application.Features.Sheets.DTOs.Requests.Workbook;

/// <summary>
///     Request to open a workbook file.
/// </summary>
public sealed record SheetWorkbookOpen(string FilePath);