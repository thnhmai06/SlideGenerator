using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
///     Request to open a sheet file.
/// </summary>
public record SheetWorkbookOpen(string FilePath) : SheetRequest(SheetRequestType.BookOpen, FilePath);