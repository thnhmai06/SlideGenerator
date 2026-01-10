using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Worksheet;

/// <summary>
///     Response containing worksheet headers.
/// </summary>
public sealed record SheetWorksheetGetHeadersSuccess(
    string FilePath,
    string SheetName,
    IReadOnlyList<string?> Headers)
    : Response("getheaders");