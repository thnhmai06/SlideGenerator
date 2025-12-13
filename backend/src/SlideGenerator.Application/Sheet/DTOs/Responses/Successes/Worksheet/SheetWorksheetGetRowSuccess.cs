using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Worksheet;

/// <summary>
/// Response containing a row of data.
/// </summary>
public record SheetWorksheetGetRowSuccess(
    string FilePath,
    string SheetName,
    int RowNumber,
    Dictionary<string, string?> RowData) : SheetSuccess(FilePath, SheetRequestType.SheetRow);