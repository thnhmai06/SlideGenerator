namespace SlideGenerator.Domain.Sheets.Models;

public record WorksheetIdentifier(WorkbookIdentifier Workbook, string Name)
{
    public ColumnIdentifier GetColumn(string columnName)
    {
        return new ColumnIdentifier(this, columnName);
    }
}