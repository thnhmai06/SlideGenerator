using SlideGenerator.Domain.Sheet.Interfaces;

namespace SlideGenerator.Application.Sheet.Contracts;

/// <summary>
/// Interface for sheet processing service.
/// </summary>
public interface ISheetService
{
    ISheetBook OpenFile(string filePath);
    Dictionary<string, int> GetSheets(ISheetBook group);
    IReadOnlyList<string?> GetHeaders(ISheetBook group, string tableName);
    Dictionary<string, string?> GetRow(ISheetBook group, string tableName, int rowNumber);
}