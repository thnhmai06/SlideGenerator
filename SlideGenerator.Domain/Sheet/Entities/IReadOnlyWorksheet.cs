namespace SlideGenerator.Domain.Sheet.Entities;

public interface IReadOnlyWorksheet
{
    IReadOnlyList<string> GetHeadersName();
    int GetRowsCount();
    IReadOnlyDictionary<string, string> GetRowContent(int rowIndex);
}