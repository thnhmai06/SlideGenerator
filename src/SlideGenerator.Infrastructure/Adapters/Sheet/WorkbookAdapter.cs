using SlideGenerator.Domain.Sheet.Interfaces;
using CoreWorkbook = SlideGenerator.Framework.Sheet.Models.Workbook;

namespace SlideGenerator.Infrastructure.Adapters.Sheet;

/// <summary>
/// Adapter to convert SlideGenerator.Framework.Sheet.Models.Workbook to Domain.Sheet.Interfaces.ISheetBook.
/// </summary>
internal sealed class WorkbookAdapter : ISheetBook
{
    private readonly CoreWorkbook _workbook;
    private readonly Dictionary<string, ISheet> _sheets;
    private bool _disposed;

    public WorkbookAdapter(CoreWorkbook workbook)
    {
        _workbook = workbook;
        _sheets = workbook.Worksheets.ToDictionary(
            kv => kv.Key,
            kv => (ISheet)new WorksheetAdapter(kv.Value));
    }

    public string FilePath => _workbook.FilePath;
    public string? Name => _workbook.Name;
    public IReadOnlyDictionary<string, ISheet> Sheets => _sheets;

    public Dictionary<string, int> GetSheetsInfo()
    {
        return _workbook.GetWorksheetsInfo();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _workbook.Dispose();
        GC.SuppressFinalize(this);
    }
}
