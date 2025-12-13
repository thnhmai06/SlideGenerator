using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Sheet.Contracts;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Infrastructure.Adapters.Sheet;
using SlideGenerator.Infrastructure.Exceptions.Sheet;
using SlideGenerator.Infrastructure.Services.Base;
using CoreWorkbook = SlideGenerator.Framework.Sheet.Models.Workbook;

namespace SlideGenerator.Infrastructure.Services.Sheet;

/// <summary>
/// Sheet processing service implementation.
/// </summary>
public class SheetService(ILogger<SheetService> logger) : Service(logger),
    ISheetService
{
    public ISheetBook OpenFile(string filePath)
    {
        Logger.LogInformation("Opening sheet file: {FilePath}", filePath);
        var workbook = new CoreWorkbook(filePath);
        return new WorkbookAdapter(workbook);
    }

    public Dictionary<string, int> GetSheets(ISheetBook group)
    {
        return group.GetSheetsInfo();
    }

    public IReadOnlyList<string?> GetHeaders(ISheetBook group, string tableName)
    {
        return !group.Sheets.TryGetValue(tableName, out var table)
            ? throw new TableNotFoundException(tableName, group.FilePath)
            : table.Headers;
    }

    public Dictionary<string, string?> GetRow(ISheetBook group, string tableName, int rowNumber)
    {
        return !group.Sheets.TryGetValue(tableName, out var table)
            ? throw new TableNotFoundException(tableName, group.FilePath)
            : table.GetRow(rowNumber);
    }
}