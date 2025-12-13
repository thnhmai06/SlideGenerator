using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
/// Request to get workbook information.
/// </summary>
public record GetWorkbookInfoRequest(string FilePath) : SheetRequest(SheetRequestType.BookInfo, FilePath);