namespace SlideGenerator.Domain.Sheet.Interfaces;

public interface ISheetBook : IDisposable
{
    string FilePath { get; }
    string? Name { get; }
    IReadOnlyDictionary<string, ISheet> Sheets { get; }
    Dictionary<string, int> GetSheetsInfo();
}