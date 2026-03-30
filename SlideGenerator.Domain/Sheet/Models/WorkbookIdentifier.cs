namespace SlideGenerator.Domain.Sheet.Models;

/// <summary>
///     Identifies a workbook.
/// </summary>
/// <param name="FilePath">File path to an Excel workbook file.</param>
public record WorkbookIdentifier(string FilePath);