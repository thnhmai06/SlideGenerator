using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Worksheet;

/// <summary>
///     Response containing worksheet row data.
/// </summary>
public sealed record SheetWorksheetGetRowSuccess(
    string FilePath,
    string TableName,
    int RowNumber,
    Dictionary<string, string?> Row)
    : Response("getrow");