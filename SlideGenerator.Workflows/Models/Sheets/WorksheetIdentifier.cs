namespace SlideGenerator.Sheets.Models;

/// <summary>
///     Identifies a specific worksheet within a workbook.
/// </summary>
/// <param name="Workbook">The identifier of the parent workbook.</param>
/// <param name="Name">The exact name of the worksheet.</param>
public sealed record WorksheetIdentifier(WorkbookIdentifier Workbook, string Name)
{
    /// <summary>
    ///     Creates a child <see cref="ColumnIdentifier" /> for the specified column name.
    /// </summary>
    /// <param name="columnName">The exact name of the column header.</param>
    /// <returns>A new <see cref="ColumnIdentifier" /> linked to this worksheet.</returns>
    public ColumnIdentifier GetColumn(string columnName)
    {
        return new ColumnIdentifier(this, columnName);
    }
}
