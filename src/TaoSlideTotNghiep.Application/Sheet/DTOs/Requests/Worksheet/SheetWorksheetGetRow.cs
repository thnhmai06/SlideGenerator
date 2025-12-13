using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Requests.Worksheet;

/// <summary>
/// Request to get a specific row from a table.
/// </summary>
public record SheetWorksheetGetRow(string FilePath, string TableName, int RowNumber)
    : SheetRequest(SheetRequestType.SheetRow, FilePath);