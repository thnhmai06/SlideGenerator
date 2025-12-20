namespace SlideGenerator.Domain.Sheet.Interfaces;

public interface ISheetBook
{
    string FilePath { get; }
    string? Name { get; }
    IReadOnlyDictionary<string, ISheet> Worksheets { get; }
    IReadOnlyDictionary<string, int> GetSheetsInfo();
    void Close();
}