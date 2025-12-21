namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
///     Request to open a workbook file.
/// </summary>
public sealed record SheetWorkbookOpen(string FilePath);