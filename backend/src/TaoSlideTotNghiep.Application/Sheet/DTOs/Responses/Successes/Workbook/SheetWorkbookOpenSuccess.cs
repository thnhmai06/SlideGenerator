using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
/// Response for opening a sheet file.
/// </summary>
public record OpenBookSheetSuccess(string FilePath) : SheetSuccess(FilePath, SheetRequestType.BookOpen);