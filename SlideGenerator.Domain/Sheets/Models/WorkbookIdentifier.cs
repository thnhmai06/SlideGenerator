namespace SlideGenerator.Domain.Sheets.Models;

/// <summary>
///     Identifies a spreadsheet workbook on the file system.
/// </summary>
/// <param name="FilePath">The absolute or relative file path to the workbook file.</param>
public record WorkbookIdentifier(string FilePath)
{
    /// <summary>Gets the name of the workbook file without its extension.</summary>
    public string Name => Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>Gets the file path to the workbook file.</summary>
    /// <exception cref="ArgumentException">Thrown when the provided file path is null or whitespace.</exception>
    public string FilePath { get; init; } = string.IsNullOrWhiteSpace(FilePath)
        ? throw new ArgumentException("File path cannot be null or whitespace.", nameof(FilePath))
        : FilePath;

    /// <summary>
    ///     Creates a child <see cref="WorksheetIdentifier" /> for the specified worksheet name.
    /// </summary>
    /// <param name="name">The name of the worksheet.</param>
    /// <returns>A new <see cref="WorksheetIdentifier" /> linked to this workbook.</returns>
    public WorksheetIdentifier GetWorksheet(string name)
    {
        return new WorksheetIdentifier(this, name);
    }
}
