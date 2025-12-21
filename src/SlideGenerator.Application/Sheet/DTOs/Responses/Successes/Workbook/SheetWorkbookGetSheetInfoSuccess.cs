using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response containing worksheet counts.
/// </summary>
public sealed record SheetWorkbookGetSheetInfoSuccess(
    string FilePath,
    IReadOnlyDictionary<string, int> Sheets)
    : Response("gettables");