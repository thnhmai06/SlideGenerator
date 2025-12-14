using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response containing Sheet information.
/// </summary>
public record SheetWorkbookGetSheetInfoSuccess(string FilePath, Dictionary<string, int> Sheets)
    : SheetSuccess(FilePath, SheetRequestType.BookSheets);