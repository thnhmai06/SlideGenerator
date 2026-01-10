using SlideGenerator.Domain.Features.Sheets.Interfaces;

namespace SlideGenerator.Application.Features.Sheets;

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