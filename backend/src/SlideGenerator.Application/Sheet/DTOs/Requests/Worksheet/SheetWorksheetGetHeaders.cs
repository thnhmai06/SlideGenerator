using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Requests.Worksheet;

/// <summary>
/// Request to get table headers.
/// </summary>
public record SheetWorksheetGetHeaders(string FilePath, string SheetName)
    : SheetRequest(SheetRequestType.SheetHeaders, FilePath);