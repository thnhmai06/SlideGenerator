namespace SlideGenerator.Domain.Sheet.Models;

public record WorksheetIdentifier(WorkbookIdentifier Workbook, string Name)
{
    public ColumnIdentifier GetColumn(string columnName) => new(this, columnName);
}