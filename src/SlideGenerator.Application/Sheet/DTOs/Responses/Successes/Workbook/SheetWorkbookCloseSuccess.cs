using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response indicating a workbook has been closed.
/// </summary>
public sealed record SheetWorkbookCloseSuccess(string FilePath) : Response("closefile");