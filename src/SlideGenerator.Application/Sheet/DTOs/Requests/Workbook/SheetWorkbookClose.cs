using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
/// Request to close a sheet file.
/// </summary>
public record SheetWorkbookClose(string FilePath) : SheetRequest(SheetRequestType.BookClose, FilePath);