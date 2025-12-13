using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
/// Response for opening a sheet file.
/// </summary>
public record OpenBookSheetSuccess(string FilePath) : SheetSuccess(FilePath, SheetRequestType.BookOpen);