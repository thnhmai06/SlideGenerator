using ClosedXML.Excel;
using System.Collections.Concurrent;
using TaoSlideTotNghiep.Domain.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Config;
using TaoSlideTotNghiep.Infrastructure.Exceptions;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Models;

/// <summary>
/// Represents a workbook file and its sheets.
/// </summary>
public class Workbook : EngineModel
    , IWorkbook
{
    private readonly XLWorkbook _workbook;
    private readonly ConcurrentDictionary<string, IWorksheet> _sheets;
    private bool _disposed;

    public string FilePath { get; }

    /// <summary>
    /// Initializes a Workbook by loading the workbook from the specified file path.
    /// </summary>
    public Workbook(string filePath)
    {
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        if (!ConfigModel.SpreadsheetExtensions.Contains(ext))
            throw new FileExtensionNotSupportedException(ext);

        FilePath = filePath;
        _workbook = new XLWorkbook(filePath);
        _sheets = new ConcurrentDictionary<string, IWorksheet>();

        // Load all sheets as tables
        foreach (var worksheet in _workbook.Worksheets)
            _sheets[worksheet.Name] = new Worksheet(worksheet);
    }

    /// <summary>
    /// Gets all tables in the workbook.
    /// </summary>
    public IReadOnlyDictionary<string, IWorksheet> Sheets => _sheets;

    /// <summary>
    /// Gets the workbook name.
    /// </summary>
    public string? Name => _workbook.Properties.Title;

    /// <summary>
    /// Gets table names with their row counts.
    /// </summary>
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

/// <summary>
/// Represents a worksheet (table) in a workbook.
/// </summary>
public class Worksheet : EngineModel, IWorksheet
{
    private readonly IXLWorksheet _worksheet;
    private readonly int _minRow;
    private readonly int _maxRow;
    private readonly int _minCol;
    private readonly int _maxCol;
    private readonly List<string?> _headers;

    public string Name => _worksheet.Name;
    public IReadOnlyList<string?> Headers => _headers;
    public int RowCount => _maxRow - _minRow;

    public Worksheet(IXLWorksheet worksheet)
    {
        _worksheet = worksheet;

        // Find the used range bounds
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            _minRow = _maxRow = _minCol = _maxCol = 1;
            _headers = [];
            return;
        }

        _minRow = usedRange.FirstRow().RowNumber();
        _maxRow = usedRange.LastRow().RowNumber();
        _minCol = usedRange.FirstColumn().ColumnNumber();
        _maxCol = usedRange.LastColumn().ColumnNumber();

        // Extract headers from first row
        _headers = [];
        for (var col = _minCol; col <= _maxCol; col++)
        {
            var cellValue = worksheet.Cell(_minRow, col).GetValue<string>();
            _headers.Add(cellValue);
        }
    }

    /// <summary>
    /// Gets a row by its number (1-based, relative to data rows).
    /// </summary>
    public Dictionary<string, string?> GetRow(int rowNumber)
    {
        if (rowNumber < 1 || rowNumber > RowCount)
            throw new IndexOutOfRangeException($"Index {rowNumber} is out of range [1, {RowCount}]");

        var actualRow = _minRow + rowNumber; // Skip header row
        var rowData = new Dictionary<string, string?>();

        for (var col = _minCol; col <= _maxCol; col++)
        {
            var header = _headers[col - _minCol];
            var cellValue = _worksheet.Cell(actualRow, col).Value;
            var value = cellValue.ToString();
            rowData[header ?? $"Column{col}"] = value;
        }

        return rowData;
    }

    /// <summary>
    /// Gets all rows as a list of dictionaries.
    /// </summary>
    public List<Dictionary<string, string?>> GetAllRows()
    {
        var rows = new List<Dictionary<string, string?>>();
        for (var i = 1; i <= RowCount; i++)
            rows.Add(GetRow(i));

        return rows;
    }
}