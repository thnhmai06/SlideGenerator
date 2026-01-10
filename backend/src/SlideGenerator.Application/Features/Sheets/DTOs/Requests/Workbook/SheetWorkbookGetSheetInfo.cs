namespace SlideGenerator.Application.Features.Sheets.DTOs.Requests.Workbook;

/// <summary>
///     Request to retrieve sheet information for a workbook.
/// </summary>
public sealed record SheetWorkbookGetSheetInfo(string FilePath);