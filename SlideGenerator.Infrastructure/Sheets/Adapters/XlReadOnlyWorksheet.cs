using ClosedXML.Excel;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Infrastructure.Sheets.Adapters;

/// <summary>
///     Represents an Excel worksheet implemented using ClosedXML for read-only access.
/// </summary>
public class XlReadOnlyWorksheet : IReadOnlyWorksheet
{
    /// <summary>The core ClosedXML worksheet instance.</summary>
    private readonly IXLWorksheet _core;

    /// <summary>
    ///     Initializes a new instance of the <see cref="XlReadOnlyWorksheet" /> class.
    /// </summary>
    /// <param name="workbook">The parent read-only workbook.</param>
    /// <param name="core">The underlying ClosedXML worksheet.</param>
    internal XlReadOnlyWorksheet(XlReadOnlyWorkbook workbook, IXLWorksheet core)
    {
        Workbook = workbook;
        _core = core;

        Headers = ContentRange?.FirstRow().Cells()
            .Select(cell => cell.GetString())
            .ToList() ?? [];
    }

    /// <summary>Gets the range of cells containing data.</summary>
    private IXLRange? ContentRange => _core.RangeUsed(XLCellsUsedOptions.Contents);

    /// <inheritdoc />
    public IReadOnlyWorkbook Workbook { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Headers { get; }

    /// <inheritdoc />
    public WorksheetIdentifier Identifier => new(Workbook.Identifier, _core.Name);

    /// <inheritdoc />
    public int RowsCount => Math.Max(0, ContentRange?.RowCount() - 1 ?? 0);

    /// <inheritdoc />
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
