using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SfXlsIO = Syncfusion.XlsIO;

namespace SlideGenerator.Infrastructure.Sheets.Adapters;

/// <summary>
///     Represents a read-only Excel workbook backed by the Syncfusion XlsIO library.
///     Workbook and engine are lazily initialised and disposed together.
/// </summary>
public sealed class SfReadOnlyWorkbook : IReadOnlyWorkbook
{
    /// <summary>
    ///     The Syncfusion Excel engine instance.
    /// </summary>
    private SfXlsIO.ExcelEngine? _engine;

    /// <summary>
    ///     The Syncfusion workbook instance.
    /// </summary>
    private SfXlsIO.IWorkbook? _workbook;

    /// <summary>
    ///     The file stream for the workbook.
    /// </summary>
    private FileStream? _fileStream;

    /// <summary>
    ///     The lazy initializer for the workbook.
    /// </summary>
    private readonly Lazy<SfXlsIO.IWorkbook> _workbookLazy;

    /// <summary>
    ///     The cache of worksheet adapters, keyed by worksheet name.
    /// </summary>
    private readonly ConcurrentDictionary<string, SfReadOnlyWorksheet> _worksheetCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Initializes a new instance of the <see cref="SfReadOnlyWorkbook" /> class.
    /// </summary>
    /// <param name="filePath">The path to the Excel file.</param>
    public SfReadOnlyWorkbook(string filePath)
    {
        Identifier = new WorkbookIdentifier(filePath);

        _workbookLazy = new Lazy<SfXlsIO.IWorkbook>(() =>
        {
            _engine = new SfXlsIO.ExcelEngine();
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _workbook = _engine.Excel.Workbooks.Open(_fileStream, SfXlsIO.ExcelOpenType.Automatic);
            return _workbook;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        // Eagerly enumerate worksheets so Worksheets property is always valid
        Worksheets = EnumerateWorksheets().ToList();
    }

    /// <summary>
    ///     Gets the underlying Syncfusion workbook instance.
    /// </summary>
    private SfXlsIO.IWorkbook Core => _workbookLazy.Value;

    /// <inheritdoc />
    public WorkbookIdentifier Identifier { get; }

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyWorksheet> Worksheets { get; }

    /// <inheritdoc />
    public bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet)
    {
        if (_worksheetCache.TryGetValue(name, out var cached))
        {
            readOnlyWorksheet = cached;
            return true;
        }

        // Try to find by name (XlsIO worksheets are 0-based indexed by name via IWorksheets[name])
        SfXlsIO.IWorksheet? coreSheet = null;
        for (var i = 0; i < Core.Worksheets.Count; i++)
        {
            if (string.Equals(Core.Worksheets[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                coreSheet = Core.Worksheets[i];
                break;
            }
        }

        if (coreSheet == null)
        {
            readOnlyWorksheet = null;
            return false;
        }

        readOnlyWorksheet = _worksheetCache.GetOrAdd(name, _ => new SfReadOnlyWorksheet(this, coreSheet));
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _workbook?.Close();
        _engine?.Dispose();
        _fileStream?.Dispose();
    }

    /// <summary>
    ///     Enumerates all worksheets in the workbook and caches their adapters.
    /// </summary>
    /// <returns>An enumeration of <see cref="IReadOnlyWorksheet" /> instances.</returns>
    private IEnumerable<IReadOnlyWorksheet> EnumerateWorksheets()
    {
        for (var i = 0; i < Core.Worksheets.Count; i++)
        {
            var sheet = Core.Worksheets[i];
            yield return _worksheetCache.GetOrAdd(
                sheet.Name,
                _ => new SfReadOnlyWorksheet(this, sheet));
        }
    }
}
