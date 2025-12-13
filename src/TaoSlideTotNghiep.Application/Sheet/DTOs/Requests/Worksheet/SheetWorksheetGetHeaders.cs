using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Requests.Worksheet;

/// <summary>
/// Request to get table headers.
/// </summary>
public record SheetWorksheetGetHeaders(string FilePath, string SheetName)
    : SheetRequest(SheetRequestType.SheetHeaders, FilePath);