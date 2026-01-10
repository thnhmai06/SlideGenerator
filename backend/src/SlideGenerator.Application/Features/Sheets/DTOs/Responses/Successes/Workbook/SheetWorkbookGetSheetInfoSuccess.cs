using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response containing worksheet counts.
/// </summary>
public sealed record SheetWorkbookGetSheetInfoSuccess(
    string FilePath,
    IReadOnlyDictionary<string, int> Sheets)
    : Response("gettables");