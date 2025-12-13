using ClosedXML.Excel;
using System.Collections.Concurrent;
using TaoSlideTotNghiep.Application.Configs.Models;
using TaoSlideTotNghiep.Domain.Sheet.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Sheet;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Sheet.Models;

public class SheetBook : Model, ISheetBook
{
    private readonly XLWorkbook _workbook;
    private readonly ConcurrentDictionary<string, ISheet> _sheets;
    private bool _disposed;

    public string FilePath { get; }
    public string? Name => _workbook.Properties.Title;
    public IReadOnlyDictionary<string, ISheet> Sheets => _sheets;

    public SheetBook(string filePath)
    {
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        if (!Config.SpreadsheetExtensions.Contains(ext))
            throw new FileExtensionNotSupportedException(ext);

        FilePath = filePath;
        _workbook = new XLWorkbook(filePath);
        _sheets = new ConcurrentDictionary<string, ISheet>();

        foreach (var worksheet in _workbook.Worksheets)
            _sheets[worksheet.Name] = new Sheet(worksheet);
    }

    public Dictionary<string, int> GetSheetsInfo()
    {
        return _sheets.ToDictionary(t => t.Key, t => t.Value.RowCount);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _workbook.Dispose();
        GC.SuppressFinalize(this);
    }
}