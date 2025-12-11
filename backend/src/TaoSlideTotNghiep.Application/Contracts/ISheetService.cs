using TaoSlideTotNghiep.Domain.Interfaces;

namespace TaoSlideTotNghiep.Application.Contracts;

/// <summary>
/// Interface for sheet processing service.
/// </summary>
public interface ISheetService
{
    IWorkbook OpenFile(string filePath);
    Dictionary<string, int> GetSheets(IWorkbook group);
    IReadOnlyList<string?> GetHeaders(IWorkbook group, string tableName);
    Dictionary<string, string?> GetRow(IWorkbook group, string tableName, int rowNumber);
}