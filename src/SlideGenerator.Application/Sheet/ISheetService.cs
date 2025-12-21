using SlideGenerator.Domain.Sheet.Interfaces;

namespace SlideGenerator.Application.Sheet;

using RowContent = Dictionary<string, string?>;

/// <summary>
///     Interface for sheet processing service.
/// </summary>
public interface ISheetService
{
    ISheetBook OpenFile(string filePath);
    IReadOnlyDictionary<string, int> GetSheetsInfo(ISheetBook group);
    IReadOnlyList<string?> GetHeaders(ISheetBook group, string tableName);
    RowContent GetRow(ISheetBook group, string tableName, int rowNumber);
}