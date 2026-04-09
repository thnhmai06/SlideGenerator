using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Domain.Sheets.Entities;

public interface IReadOnlyWorksheet
{
    IReadOnlyWorkbook Workbook { get; }
    WorksheetIdentifier Identifier { get; }
    string Name => Identifier.Name;
    IReadOnlyList<string> Headers { get; }
    int RowsCount { get; }
    IReadOnlyDictionary<string, string> GetRowContent(int rowIndex);
}