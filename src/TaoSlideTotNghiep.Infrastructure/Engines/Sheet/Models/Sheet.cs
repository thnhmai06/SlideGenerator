using ClosedXML.Excel;
using TaoSlideTotNghiep.Domain.Sheet.Interfaces;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Sheet.Models;

public class Sheet : Model, ISheet
{
    private readonly IXLWorksheet _worksheet;
    private readonly int _minRow;
    private readonly int _minCol;
    private readonly int _maxCol;
    private readonly List<string?> _headers;

    public string Name => _worksheet.Name;
    public IReadOnlyList<string?> Headers => _headers;
    public int RowCount => field - _minRow;

    public Sheet(IXLWorksheet worksheet)
    {
        _worksheet = worksheet;

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            _minRow = RowCount = _minCol = _maxCol = 1;
            _headers = [];
            return;
        }

        _minRow = usedRange.FirstRow().RowNumber();
        RowCount = usedRange.LastRow().RowNumber();
        _minCol = usedRange.FirstColumn().ColumnNumber();
        _maxCol = usedRange.LastColumn().ColumnNumber();

        _headers = [];
        for (var col = _minCol; col <= _maxCol; col++)
        {
            var cellValue = worksheet.Cell(_minRow, col).GetValue<string>();
            _headers.Add(cellValue);
        }
    }

    public Dictionary<string, string?> GetRow(int rowNumber)
    {
        if (rowNumber < 1 || rowNumber > RowCount)
            throw new IndexOutOfRangeException($"Index {rowNumber} is out of range [1, {RowCount}]");

        var actualRow = _minRow + rowNumber;
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

    public List<Dictionary<string, string?>> GetAllRows()
    {
        var rows = new List<Dictionary<string, string?>>();
        for (var i = 1; i <= RowCount; i++)
            rows.Add(GetRow(i));

        return rows;
    }
}