namespace SlideGenerator.Documents.Sheets.Models;

/// <summary>
///     Uniquely identifies a specific column within an Excel worksheet.
/// </summary>
/// <param name="BookPath">The path to the workbook.</param>
/// <param name="SheetName">The name of the worksheet.</param>
/// <param name="ColumnName">The name (header) of the column.</param>
/// <param name="BookPassword">Optional password for the workbook.</param>
public record ColumnIdentifier(string BookPath, string SheetName, string ColumnName, string? BookPassword = null)
    : SheetIdentifier(BookPath, SheetName, BookPassword);