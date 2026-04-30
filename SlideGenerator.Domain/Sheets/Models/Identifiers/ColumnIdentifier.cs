namespace SlideGenerator.Domain.Sheets.Models.Identifiers;

/// <summary>
///     Identifies a specific column within a worksheet.
/// </summary>
/// <param name="Worksheet">The identifier of the parent worksheet.</param>
/// <param name="Name">The exact name of the column header.</param>
public sealed record ColumnIdentifier(WorksheetIdentifier Worksheet, string Name);