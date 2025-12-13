namespace SlideGenerator.Domain.Sheet.Interfaces;

public interface ISheet
{
    string Name { get; }
    IReadOnlyList<string?> Headers { get; }
    int RowCount { get; }
    Dictionary<string, string?> GetRow(int rowNumber);
    List<Dictionary<string, string?>> GetAllRows();
}