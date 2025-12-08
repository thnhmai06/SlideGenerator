using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Models;

namespace TaoSlideTotNghiep.Services;

/// <summary>
/// Interface for sheet processing service.
/// </summary>
public interface ISheetService
{
    Workbook OpenFile(string filePath);
    Dictionary<string, int> GetTables(Workbook group);
    IReadOnlyList<string?> GetHeaders(Workbook group, string tableName);
    Dictionary<string, object?> GetRow(Workbook group, string tableName, int rowNumber);
}

/// <summary>
/// Sheet processing service implementation.
/// </summary>
public class SheetService(ILogger<SheetService> logger) : Service(logger), ISheetService
{
    public Workbook OpenFile(string filePath)
    {
        Logger.LogInformation("Opening sheet file: {FilePath}", filePath);
        return new Workbook(filePath);
    }

    public Dictionary<string, int> GetTables(Workbook group)
    {
        return group.GetTableInfo();
    }

    public IReadOnlyList<string?> GetHeaders(Workbook group, string tableName)
    {
        return !group.Sheets.TryGetValue(tableName, out var table)
            ? throw new TableNotFoundException(tableName, group.FilePath)
            : table.Headers;
    }

    public Dictionary<string, object?> GetRow(Workbook group, string tableName, int rowNumber)
    {
        return !group.Sheets.TryGetValue(tableName, out var table)
            ? throw new TableNotFoundException(tableName, group.FilePath)
            : table.GetRow(rowNumber);
    }
}