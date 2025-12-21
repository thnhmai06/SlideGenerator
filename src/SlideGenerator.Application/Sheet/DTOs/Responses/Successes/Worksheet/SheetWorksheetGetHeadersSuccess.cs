using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Worksheet;

/// <summary>
///     Response containing worksheet headers.
/// </summary>
public sealed record SheetWorksheetGetHeadersSuccess(
    string FilePath,
    string SheetName,
    IReadOnlyList<string?> Headers)
    : Response("getheaders");