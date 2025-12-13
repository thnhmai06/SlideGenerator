using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
/// Request to open a sheet file.
/// </summary>
public record SheetWorkbookOpen(string FilePath) : SheetRequest(SheetRequestType.BookOpen, FilePath);