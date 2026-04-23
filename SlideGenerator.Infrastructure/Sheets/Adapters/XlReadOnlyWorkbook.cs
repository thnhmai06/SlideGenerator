using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ClosedXML.Excel;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Infrastructure.Sheets.Adapters;

/// <summary>
///     Represents an Excel workbook implemented using ClosedXML for read-only access.
/// </summary>
public class XlReadOnlyWorkbook : IReadOnlyWorkbook
{
    /// <summary>Lazily loads the underlying ClosedXML workbook instance.</summary>
    private readonly Lazy<IXLWorkbook> _workbookLazy;

    /// <summary>Caches created read-only worksheet instances.</summary>
    private readonly ConcurrentDictionary<IXLWorksheet, XlReadOnlyWorksheet> _worksheetCache = new();

    /// <summary>Holds the active file stream reading the workbook data.</summary>
    private FileStream? _fileStream;

    /// <summary>
    ///     Initializes a new instance of the <see cref="XlReadOnlyWorkbook" /> class.
    /// </summary>
    /// <param name="filePath">The file path to the Excel workbook.</param>
    public XlReadOnlyWorkbook(string filePath)
    {
        Identifier = new WorkbookIdentifier(filePath);
        _workbookLazy = new Lazy<IXLWorkbook>(() =>
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new XLWorkbook(_fileStream);
        });

        Worksheets = EnumerateWorksheets().ToList();
    }

    /// <summary>Gets the core ClosedXML workbook instance.</summary>
    private IXLWorkbook Core => _workbookLazy.Value;

    /// <inheritdoc />
    public WorkbookIdentifier Identifier { get; }

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyWorksheet> Worksheets { get; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_workbookLazy.IsValueCreated)
            Core.Dispose();

        _fileStream?.Dispose();
    }

    /// <summary>
    ///     Enumerates the worksheets available in this workbook.
    /// </summary>
    /// <returns>A collection of <see cref="IReadOnlyWorksheet" /> instances.</returns>
    public IEnumerable<IReadOnlyWorksheet> EnumerateWorksheets()
    {
        return Core.Worksheets.Select(core => _worksheetCache.GetOrAdd(
            core,
            static (core, currentThis) => new XlReadOnlyWorksheet(currentThis, core),
            this
        ));
    }
}