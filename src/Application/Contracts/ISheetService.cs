using Domain.Models;

namespace Application.Contracts;

/// <summary>
/// Interface for sheet processing service.
/// </summary>
public interface ISheetService
{
    Workbook OpenFile(string filePath);
    Dictionary<string, int> GetSheets(Workbook group);
    IReadOnlyList<string?> GetHeaders(Workbook group, string tableName);
    Dictionary<string, object?> GetRow(Workbook group, string tableName, int rowNumber);
}