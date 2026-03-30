namespace SlideGenerator.Domain.Sheet.Models;

/// <summary>
///     Identifies a workbook.
/// </summary>
/// <param name="FilePath">File path to an Excel workbook file.</param>
public record WorkbookIdentifier(string FilePath)
{
    public string Name => Path.GetFileNameWithoutExtension(FilePath);

    public string FilePath { get; init; } = string.IsNullOrWhiteSpace(FilePath)
        ? throw new ArgumentException("File path cannot be null or whitespace.", nameof(FilePath))
        : FilePath;
    
    public WorksheetIdentifier GetWorksheet(string name) => new(this, name);
}