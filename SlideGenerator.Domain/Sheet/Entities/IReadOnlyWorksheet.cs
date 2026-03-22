namespace SlideGenerator.Domain.Sheet.Entities;

public interface IReadOnlyWorksheet
{
    IReadOnlyList<string> GetHeadersName();
    IReadOnlyDictionary<string, string> GetRowContent(int rowIndex);
}