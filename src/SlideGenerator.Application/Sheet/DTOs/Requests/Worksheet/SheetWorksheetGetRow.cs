namespace SlideGenerator.Application.Sheet.DTOs.Requests.Worksheet;

/// <summary>
///     Request to retrieve a row from a worksheet.
/// </summary>
public sealed record SheetWorksheetGetRow(string FilePath, string TableName, int RowNumber);