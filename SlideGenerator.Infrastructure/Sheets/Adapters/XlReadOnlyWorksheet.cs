/* LEGACY-CLOSEDXML — replaced by SfReadOnlyWorksheet (Syncfusion.XlsIO.NET)
using ClosedXML.Excel;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Infrastructure.Sheets.Adapters;

public class XlReadOnlyWorksheet : IReadOnlyWorksheet
{
    private readonly IXLWorksheet _core;

    internal XlReadOnlyWorksheet(XlReadOnlyWorkbook workbook, IXLWorksheet core)
    {
        Workbook = workbook;
        _core = core;

        Headers = ContentRange?.FirstRow().Cells()
            .Select(cell => cell.GetString())
            .ToList() ?? [];
    }

    private IXLRange? ContentRange => _core.RangeUsed(XLCellsUsedOptions.Contents);

    public IReadOnlyWorkbook Workbook { get; }
    public IReadOnlyList<string> Headers { get; }
    public WorksheetIdentifier Identifier => new(Workbook.Identifier, _core.Name);
    public int RowsCount => Math.Max(0, ContentRange?.RowCount() - 1 ?? 0);

    public IReadOnlyDictionary<string, string> GetRowContent(int rowIndex)
    {
        var contentRange = ContentRange;
        if (contentRange == null)
            return new Dictionary<string, string>();

        var headerCells = contentRange.FirstRow().Cells();
        var dataCells = contentRange.Row(rowIndex + 1).Cells();
        return headerCells.Zip(dataCells, (header, cell) => new
            {
                Key = header.GetString(),
                Value = cell.GetString()
            })
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key))
            .DistinctBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
*/
