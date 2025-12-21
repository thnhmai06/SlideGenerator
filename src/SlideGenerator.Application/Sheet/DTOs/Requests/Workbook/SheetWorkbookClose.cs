namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
///     Request to close a workbook file.
/// </summary>
public sealed record SheetWorkbookClose(string FilePath);