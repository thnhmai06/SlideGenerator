namespace SlideGenerator.Domain.Sheets.Models.Identifiers;

/// <summary>
///     Carries both the worksheet identifier and row index as a single ForEach item,
///     so row-level activities can access both without shared mutable context fields.
/// </summary>
/// <param name="Worksheet">The worksheet being processed.</param>
/// <param name="Index">The 1-based row index within the worksheet.</param>
public sealed record RowIdentifier(WorksheetIdentifier Worksheet, int Index);