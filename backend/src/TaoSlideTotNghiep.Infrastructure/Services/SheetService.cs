using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Contracts;
using TaoSlideTotNghiep.Domain.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Engines.Models;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Services;

namespace TaoSlideTotNghiep.Infrastructure.Services;

/// <summary>
/// Sheet processing service implementation.
/// </summary>
public class SheetService(ILogger<SheetService> logger) : Service(logger),
    ISheetService
{
    public IWorkbook OpenFile(string filePath)
    {
        Logger.LogInformation("Opening sheet file: {FilePath}", filePath);
        return new Workbook(filePath);
    }

    public Dictionary<string, int> GetSheets(IWorkbook group)
    {
        return group.GetSheetsInfo();
    }

    public IReadOnlyList<string?> GetHeaders(IWorkbook group, string tableName)
    {
        return !group.Sheets.TryGetValue(tableName, out var table)
            ? throw new TableNotFoundException(tableName, group.FilePath)
            : table.Headers;
    }

    public Dictionary<string, string?> GetRow(IWorkbook group, string tableName, int rowNumber)
    {
        return !group.Sheets.TryGetValue(tableName, out var table)
            ? throw new TableNotFoundException(tableName, group.FilePath)
            : table.GetRow(rowNumber);
    }
}