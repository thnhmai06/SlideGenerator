using System.Collections.Concurrent;
using ClosedXML.Excel;
using TaoSlideTotNghiep.Config;
using TaoSlideTotNghiep.Exceptions;

namespace TaoSlideTotNghiep.Models;

/// <summary>
/// Represents a workbook file and its sheets.
/// </summary>
public class Workbook : IDisposable
{
    private readonly XLWorkbook _workbook;
    private readonly ConcurrentDictionary<string, Worksheet> _sheets;
    private bool _disposed;

    public string FilePath { get; }

    /// <summary>
    /// Initializes a Workbook by loading the workbook from the specified file path.
    /// </summary>
    public Workbook(string filePath)
    {
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        if (!AppConfig.SpreadsheetExtensions.Contains(ext))
            throw new FileExtensionNotSupportedException(ext);

        FilePath = filePath;
        _workbook = new XLWorkbook(filePath);
        _sheets = new ConcurrentDictionary<string, Worksheet>();

        // Load all sheets as tables
        foreach (var worksheet in _workbook.Worksheets)
            _sheets[worksheet.Name] = new Worksheet(worksheet);
    }

    /// <summary>
    /// Gets all tables in the workbook.
    /// </summary>
    public IReadOnlyDictionary<string, Worksheet> Sheets => _sheets;

    /// <summary>
    /// Gets the workbook name.
    /// </summary>
    public string? Name => _workbook.Properties.Title;

    /// <summary>
    /// Gets table names with their row counts.
    /// </summary>
    public Dictionary<string, int> GetTableInfo()
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
public class Worksheet
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
    /// Gets a row by index (1-based, relative to data rows).
    /// </summary>
    public Dictionary<string, object?> GetRow(int index)
    {
        if (index < 1 || index > RowCount)
            throw new Exceptions.IndexOutOfRangeException(index, (1, RowCount));

        var actualRow = _minRow + index; // Skip header row
        var rowData = new Dictionary<string, object?>();

        for (var col = _minCol; col <= _maxCol; col++)
        {
            var header = _headers[col - _minCol];
            var cellValue = _worksheet.Cell(actualRow, col).Value;

            // Convert XLCellValue to appropriate type
            object? value = cellValue.Type switch
            {
                XLDataType.Text => cellValue.GetText(),
                XLDataType.Number => cellValue.GetNumber(),
                XLDataType.Boolean => cellValue.GetBoolean(),
                XLDataType.DateTime => cellValue.GetDateTime(),
                XLDataType.TimeSpan => cellValue.GetTimeSpan(),
                _ => cellValue.ToString()
            };

            rowData[header ?? $"Column{col}"] = value;
        }

        return rowData;
    }

    /// <summary>
    /// Gets all rows as a list of dictionaries.
    /// </summary>
    public List<Dictionary<string, object?>> GetAllRows()
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var i = 1; i <= RowCount; i++)
            rows.Add(GetRow(i));

        return rows;
    }
}
