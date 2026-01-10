namespace SlideGenerator.Domain.Features.Sheets.Interfaces;

/// <summary>
///     Represents an opened workbook.
/// </summary>
public interface ISheetBook : IDisposable
{
    string FilePath { get; }
    string? Name { get; }
    IReadOnlyDictionary<string, ISheet> Worksheets { get; }
    IReadOnlyDictionary<string, int> GetSheetsInfo();
}