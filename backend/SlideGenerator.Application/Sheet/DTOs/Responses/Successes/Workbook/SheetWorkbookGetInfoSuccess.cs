using SlideGenerator.Application.Sheet.DTOs.Components;
using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response containing workbook information.
/// </summary>
public record SheetWorkbookGetInfoSuccess(string FilePath, string? BookName, List<SheetWorksheetInfo> Sheets)
    : SheetSuccess(FilePath, SheetRequestType.BookInfo);