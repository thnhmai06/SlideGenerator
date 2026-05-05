namespace SlideGenerator.Document.Sheet.Models;

/// <summary>
///     Uniquely identifies an Excel workbook file.
/// </summary>
/// <param name="BookPath">The absolute or relative path to the workbook.</param>
/// <param name="BookPassword">Optional password if the workbook is encrypted.</param>
public record BookIdentifier(string BookPath, string? BookPassword = null, string? Separator = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the workbook.
    /// </summary>
    public string BookPath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = BookPath;

    public BookType GetBookType()
    {
        return BookTypeExtensions.FromExtension(Path.GetExtension(BookPath));
    }
}