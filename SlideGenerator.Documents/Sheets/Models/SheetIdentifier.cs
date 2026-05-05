namespace SlideGenerator.Documents.Sheets.Models;

/// <summary>
///     Uniquely identifies a specific worksheet within an Excel workbook.
/// </summary>
/// <param name="BookPath">The path to the workbook.</param>
/// <param name="SheetName">The name of the worksheet.</param>
/// <param name="BookPassword">Optional password for the workbook.</param>
public record SheetIdentifier(string BookPath, string SheetName, string? BookPassword = null)
    : BookIdentifier(BookPath, BookPassword);