namespace SlideGenerator.Application.Features.Sheets.DTOs.Requests.Workbook;

/// <summary>
///     Request to retrieve workbook info including headers.
/// </summary>
public sealed record GetWorkbookInfoRequest(string FilePath);