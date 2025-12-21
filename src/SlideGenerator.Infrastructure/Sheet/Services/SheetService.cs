using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Sheet;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Sheet.Adapters;
using SlideGenerator.Infrastructure.Sheet.Exceptions;
using CoreWorkbook = SlideGenerator.Framework.Sheet.Models.Workbook;

namespace SlideGenerator.Infrastructure.Sheet.Services;

using RowContent = Dictionary<string, string?>;

/// <summary>
///     Sheet processing service implementation.
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

    public IReadOnlyDictionary<string, int> GetSheetsInfo(ISheetBook group)
    {
        return group.GetSheetsInfo();
    }

    public IReadOnlyList<string?> GetHeaders(ISheetBook group, string tableName)
    {
        return !group.Worksheets.TryGetValue(tableName, out var table)
            ? throw new SheetNotFound(tableName, group.FilePath)
            : table.Headers;
    }

    public RowContent GetRow(ISheetBook group, string tableName, int rowNumber)
    {
        return !group.Worksheets.TryGetValue(tableName, out var table)
            ? throw new SheetNotFound(tableName, group.FilePath)
            : table.GetRow(rowNumber);
    }
}