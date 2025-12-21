using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response indicating a workbook has been opened.
/// </summary>
public sealed record OpenBookSheetSuccess(string FilePath) : Response("openfile");