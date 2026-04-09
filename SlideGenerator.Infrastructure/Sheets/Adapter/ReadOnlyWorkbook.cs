using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ClosedXML.Excel;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Infrastructure.Sheets.Adapter;

public class ReadOnlyWorkbook : IReadOnlyWorkbook
{
    private readonly Lazy<IXLWorkbook> _workbookLazy;
    private FileStream? _fileStream;
    private readonly ConcurrentDictionary<IXLWorksheet, ReadOnlyWorksheet> _worksheetCache = new();
    private IXLWorkbook Core => _workbookLazy.Value;

    public ReadOnlyWorkbook(string filePath)
    {
        Identifier = new(filePath);
        _workbookLazy = new Lazy<IXLWorkbook>(() =>
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new XLWorkbook(_fileStream);
        });

        Worksheets = EnumerateWorksheets().ToList();
    }

    public WorkbookIdentifier Identifier { get; }
    public IReadOnlyList<IReadOnlyWorksheet> Worksheets { get; }

    public bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet)
    {
        if (!Core.TryGetWorksheet(name, out var core))
        {
            readOnlyWorksheet = null;
            return false;
        }

        readOnlyWorksheet = _worksheetCache.GetOrAdd(
            core,
            static (core, currentThis) => new ReadOnlyWorksheet(currentThis, core),
            this
        );
        return true;
    }

    public IEnumerable<IReadOnlyWorksheet> EnumerateWorksheets()
    {
        return Core.Worksheets.Select(core => _worksheetCache.GetOrAdd(
            core,
            static (core, currentThis) => new ReadOnlyWorksheet(currentThis, core),
            this
        ));
    }


    public void Dispose()
    {
        if (_workbookLazy.IsValueCreated)
            Core.Dispose();

        _fileStream?.Dispose();
    }
}