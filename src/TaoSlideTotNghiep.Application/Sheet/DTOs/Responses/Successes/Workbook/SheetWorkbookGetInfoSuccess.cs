using TaoSlideTotNghiep.Application.Sheet.DTOs.Components;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
/// Response containing workbook information.
/// </summary>
public record SheetWorkbookGetInfoSuccess(string FilePath, string? BookName, List<SheetWorksheetInfo> Sheets)
    : SheetSuccess(FilePath, SheetRequestType.BookInfo);