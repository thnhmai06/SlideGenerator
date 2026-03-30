using System.Diagnostics.CodeAnalysis;
using ClosedXML.Excel;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;

namespace SlideGenerator.Infrastructure.Sheet.Adapter;

public class ReadOnlyWorkbook : IReadOnlyWorkbook
{
    private readonly Lazy<IXLWorkbook> _workbookLazy;
    private FileStream? _fileStream;
    private readonly WorkbookIdentifier _identifier;

    public ReadOnlyWorkbook(string filePath)
    {
        _identifier = new(filePath);
        _workbookLazy = new Lazy<IXLWorkbook>(() =>
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new XLWorkbook(_fileStream);
        });
    }

    private IXLWorkbook Core => _workbookLazy.Value;

    // public string? Name => string.IsNullOrWhiteSpace(Core.Properties.Title)
    //     ? Path.GetFileNameWithoutExtension(_filePath)
    //     : Core.Properties.Title;
    public string Name => _identifier.Name;

    public bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet)
    {
        if (!Core.TryGetWorksheet(name, out var coreWorksheet))
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
        foreach (var worksheet in Core.Worksheets)
        {
            var contentRange = worksheet.RangeUsed(XLCellsUsedOptions.Contents);
            var name = worksheet.Name;
            var count = Math.Max(contentRange?.RowCount() - 1 ?? 0, 0);
            result[name] = count;
        }

        return result;
    }

    public void Dispose()
    {
        if (_workbookLazy.IsValueCreated)
            Core.Dispose();

        _fileStream?.Dispose();
    }
}