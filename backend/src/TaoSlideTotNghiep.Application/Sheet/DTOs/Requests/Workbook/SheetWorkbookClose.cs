using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
/// Request to close a sheet file.
/// </summary>
public record SheetWorkbookClose(string FilePath) : SheetRequest(SheetRequestType.BookClose, FilePath);