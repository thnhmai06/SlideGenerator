using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response indicating a workbook has been opened.
/// </summary>
public sealed record OpenBookSheetSuccess(string FilePath) : Response("openfile");