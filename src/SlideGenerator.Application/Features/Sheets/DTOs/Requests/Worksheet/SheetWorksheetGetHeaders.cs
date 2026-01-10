namespace SlideGenerator.Application.Features.Sheets.DTOs.Requests.Worksheet;

/// <summary>
///     Request to retrieve headers for a worksheet.
/// </summary>
public sealed record SheetWorksheetGetHeaders(string FilePath, string SheetName);