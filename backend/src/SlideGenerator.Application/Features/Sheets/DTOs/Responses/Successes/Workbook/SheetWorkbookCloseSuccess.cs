using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response indicating a workbook has been closed.
/// </summary>
public sealed record SheetWorkbookCloseSuccess(string FilePath) : Response("closefile");