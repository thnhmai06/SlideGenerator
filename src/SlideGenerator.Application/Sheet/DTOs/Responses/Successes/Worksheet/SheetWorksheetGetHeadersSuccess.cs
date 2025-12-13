using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Worksheet;

/// <summary>
/// Response containing Sheet headers.
/// </summary>
public record SheetWorksheetGetHeadersSuccess(string FilePath, string SheetName, List<string?> Headers)
    : SheetSuccess(FilePath, SheetRequestType.SheetHeaders);