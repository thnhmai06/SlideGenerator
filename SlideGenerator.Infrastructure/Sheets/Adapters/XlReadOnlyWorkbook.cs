/* LEGACY-CLOSEDXML — replaced by SfReadOnlyWorkbook (Syncfusion.XlsIO.NET)
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ClosedXML.Excel;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Infrastructure.Sheets.Adapters;

public class XlReadOnlyWorkbook : IReadOnlyWorkbook
{
    private readonly Lazy<IXLWorkbook> _workbookLazy;
    private readonly ConcurrentDictionary<IXLWorksheet, XlReadOnlyWorksheet> _worksheetCache = new();
    private FileStream? _fileStream;

    public XlReadOnlyWorkbook(string filePath)
    {
        Identifier = new WorkbookIdentifier(filePath);
        _workbookLazy = new Lazy<IXLWorkbook>(() =>
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new XLWorkbook(_fileStream);
        });

        Worksheets = EnumerateWorksheets().ToList();
    }

    private IXLWorkbook Core => _workbookLazy.Value;

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
            static (core, currentThis) => new XlReadOnlyWorksheet(currentThis, core),
            this
        );
        return true;
    }

    public void Dispose()
    {
        if (_workbookLazy.IsValueCreated)
            Core.Dispose();

        _fileStream?.Dispose();
    }

    public IEnumerable<IReadOnlyWorksheet> EnumerateWorksheets()
    {
        return Core.Worksheets.Select(core => _worksheetCache.GetOrAdd(
            core,
            static (core, currentThis) => new XlReadOnlyWorksheet(currentThis, core),
            this
        ));
    }
}
*/
