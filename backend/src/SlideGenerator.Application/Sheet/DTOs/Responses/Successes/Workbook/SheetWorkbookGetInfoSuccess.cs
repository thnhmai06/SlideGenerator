using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Sheet.DTOs.Components;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response containing workbook inspection details.
/// </summary>
public sealed record SheetWorkbookGetInfoSuccess(
    string FilePath,
    string? WorkbookName,
    IReadOnlyList<SheetWorksheetInfo> Sheets)
    : Response("getworkbookinfo");