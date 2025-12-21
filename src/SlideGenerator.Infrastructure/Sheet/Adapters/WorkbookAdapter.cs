using SlideGenerator.Domain.Sheet.Interfaces;
using CoreWorkbook = SlideGenerator.Framework.Sheet.Models.Workbook;

namespace SlideGenerator.Infrastructure.Sheet.Adapters;

/// <summary>
///     Adapter to convert <see cref="CoreWorkbook" /> to <see cref="ISheetBook" />.
/// </summary>
internal sealed class WorkbookAdapter : ISheetBook
{
    private readonly CoreWorkbook _workbook;

    public WorkbookAdapter(CoreWorkbook workbook)
    {
        _workbook = workbook;
        Worksheets = workbook.Worksheets.ToDictionary(
            kv => kv.Key, ISheet (kv) => new WorksheetAdapter(kv.Value));
    }

    public string FilePath => _workbook.FilePath;
    public string? Name => _workbook.Name;
    public IReadOnlyDictionary<string, ISheet> Worksheets { get; }

    public IReadOnlyDictionary<string, int> GetSheetsInfo()
    {
        return _workbook.GetWorksheetsInfo();
    }

    public void Close()
    {
        _workbook.Dispose();
    }
}