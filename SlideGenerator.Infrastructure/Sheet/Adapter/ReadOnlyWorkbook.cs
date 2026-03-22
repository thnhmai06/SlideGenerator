using System.Diagnostics.CodeAnalysis;
using ClosedXML.Excel;
using SlideGenerator.Domain.Sheet.Entities;

namespace SlideGenerator.Infrastructure.Sheet.Adapter;

public class ReadOnlyWorkbook(IXLWorkbook core) : IReadOnlyWorkbook
{
    public string? Name => core.Properties.Title;
    public bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet)
    {
        if (!core.TryGetWorksheet(name, out var coreWorksheet))
        {
            readOnlyWorksheet = null;
            return false;
        }
        readOnlyWorksheet = new ReadOnlyWorksheet(coreWorksheet);
        return true;
    }

    public IReadOnlyDictionary<string, int> SummarySheets()
    {
        var result = new Dictionary<string, int>();
        foreach (var worksheet in core.Worksheets)
        {
            var contentRange = worksheet.RangeUsed(XLCellsUsedOptions.Contents);
            var name = worksheet.Name;
            var count = Math.Max(contentRange?.RowCount() - 1 ?? 0, 0);
            result[name] = count;
        }
        return result;
    }

    public void Dispose() => core.Dispose();
}