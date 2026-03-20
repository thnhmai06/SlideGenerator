namespace SlideGenerator.Domain.Tasks.Models.Sheet;

/// <summary>
///     Identifies a workbook.
/// </summary>
/// <param name="FilePath">File path to an Excel workbook file.</param>
public record WorkbookIdentifier(string FilePath);