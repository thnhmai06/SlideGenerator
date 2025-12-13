using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
/// Request to get all tables in a sheet.
/// </summary>
public record SheetWorkbookGetSheetInfo(string FilePath) : SheetRequest(SheetRequestType.BookSheets, FilePath);