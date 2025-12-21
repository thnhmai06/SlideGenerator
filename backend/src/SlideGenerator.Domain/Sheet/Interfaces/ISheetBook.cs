namespace SlideGenerator.Domain.Sheet.Interfaces;

/// <summary>
///     Represents an opened workbook.
/// </summary>
public interface ISheetBook
{
    string FilePath { get; }
    string? Name { get; }
    IReadOnlyDictionary<string, ISheet> Worksheets { get; }
    IReadOnlyDictionary<string, int> GetSheetsInfo();
    void Close();
}