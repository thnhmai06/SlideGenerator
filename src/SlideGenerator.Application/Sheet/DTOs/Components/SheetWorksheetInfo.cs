namespace SlideGenerator.Application.Sheet.DTOs.Components;

/// <summary>
///     Worksheet info for workbook inspection.
/// </summary>
public sealed record SheetWorksheetInfo(string Name, IReadOnlyList<string?> Headers, int RowCount);