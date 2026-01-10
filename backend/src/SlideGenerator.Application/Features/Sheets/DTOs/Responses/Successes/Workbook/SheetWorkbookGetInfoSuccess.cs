using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Sheets.DTOs.Components;

namespace SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Workbook;

/// <summary>
///     Response containing workbook inspection details.
/// </summary>
public sealed record SheetWorkbookGetInfoSuccess(
    string FilePath,
    string? WorkbookName,
    IReadOnlyList<SheetWorksheetInfo> Sheets)
    : Response("getworkbookinfo");