using ClosedXML.Excel;
using SlideGenerator.Domain.Sheet.Entities;

namespace SlideGenerator.Infrastructure.Sheet.Adapter;

public class ReadOnlyWorksheet : IReadOnlyWorksheet
{
    internal readonly IXLWorksheet Core;
    
    internal ReadOnlyWorksheet(IXLWorksheet core)
    {
        Core = core;
    }
    
    private IXLRange? ContentRange => Core.RangeUsed(XLCellsUsedOptions.Contents);

    public IReadOnlyList<string> GetHeadersName()
    {
        return ContentRange?.FirstRow().Cells()
            .Select(cell => cell.GetString())
            .ToList() ?? [];
    }

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