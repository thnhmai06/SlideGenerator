namespace SlideGenerator.Application.Sheet.DTOs.Requests.Workbook;

/// <summary>
///     Request to retrieve workbook info including headers.
/// </summary>
public sealed record GetWorkbookInfoRequest(string FilePath);