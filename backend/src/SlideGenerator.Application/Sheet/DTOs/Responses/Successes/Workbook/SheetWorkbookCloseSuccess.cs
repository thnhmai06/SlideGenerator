using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
/// Response for closing a sheet file.
/// </summary>
public record SheetWorkbookCloseSuccess(string FilePath) : SheetSuccess(FilePath, SheetRequestType.BookClose);