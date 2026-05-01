using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Sheets.Models.Previews;
using SfXlsIO = Syncfusion.XlsIO;

namespace SlideGenerator.Infrastructure.Sheets.Adapters;

/// <summary>
///     Represents a read-only worksheet backed by the Syncfusion XlsIO library.
/// </summary>
public sealed class SfReadOnlyWorksheet : IReadOnlyWorksheet
{
    /// <summary>
    ///     The underlying Syncfusion worksheet instance.
    /// </summary>
    private readonly SfXlsIO.IWorksheet _core;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SfReadOnlyWorksheet" /> class.
    /// </summary>
    /// <param name="workbook">The parent workbook adapter.</param>
    /// <param name="core">The underlying Syncfusion worksheet instance.</param>
    internal SfReadOnlyWorksheet(SfReadOnlyWorkbook workbook, SfXlsIO.IWorksheet core)
    {
        Workbook = workbook;
        _core = core;

        Headers = ParseHeaders();
        RowsCount = ComputeRowsCount();
    }

    /// <summary>
    ///     Gets the used range of cells in the worksheet.
    /// </summary>
    private SfXlsIO.IRange? UsedRange => _core.UsedRange;

    /// <inheritdoc />
    public IReadOnlyWorkbook Workbook { get; }

    /// <inheritdoc />
    public WorksheetIdentifier Identifier => new(Workbook.Identifier, _core.Name);

    /// <inheritdoc />
    public IReadOnlyList<string> Headers { get; }

    /// <inheritdoc />
    public int RowsCount { get; }

    /// <inheritdoc />
    /// <param name="rowIndex">1-based data row index (row 1 = first row after the header).</param>
    public IReadOnlyDictionary<string, string> GetRowContent(int rowIndex)
    {
        var used = UsedRange;
        if (used == null || rowIndex <= 0 || rowIndex > RowsCount)
            return new Dictionary<string, string>();

        // In XlsIO, UsedRange.Row is the 1-based row of the first used cell.
        // Header is at used.Row; data rows start at used.Row + 1.
        var dataRowAbsolute = used.Row + rowIndex; // XlsIO absolute 1-based row index

        var result = new Dictionary<string, string>(Headers.Count, StringComparer.Ordinal);
        for (var col = 0; col < Headers.Count; col++)
        {
            var header = Headers[col];
            if (string.IsNullOrEmpty(header)) continue;

            // XlsIO column index is 1-based; used.Column is the first used column.
            var absCol = used.Column + col;
            var cell = _core.Range[dataRowAbsolute, absCol];

            // DisplayText preserves exact formatting (e.g. 8.5 stays "8.5", not "9")
            var displayText = cell?.DisplayText ?? string.Empty;

            if (!result.ContainsKey(header))
                result[header] = displayText;
        }

        return result;
    }

    /// <summary>
    ///     Parses the headers from the first used row of the worksheet.
    /// </summary>
    /// <returns>A list of header names.</returns>
    private IReadOnlyList<string> ParseHeaders()
    {
        var used = UsedRange;
        if (used == null) return [];

        var headerRow = used.Row; // absolute 1-based row
        var headers = new List<string>();

        for (var col = used.Column; col <= used.LastColumn; col++)
        {
            var cell = _core.Range[headerRow, col];
            headers.Add(cell?.DisplayText ?? string.Empty);
        }

        return headers;
    }

    /// <summary>
    ///     Computes the number of data rows in the worksheet (excluding the header row).
    /// </summary>
    /// <returns>The number of data rows.</returns>
    private int ComputeRowsCount()
    {
        var used = UsedRange;
        return used != null ? Math.Max(0, used.LastRow - used.Row) : 0;
    }

    /// <inheritdoc />
    public Task<WorksheetPreview> GetPreview(
        int from = 1,
        int to = 10,
        bool skipPreview = false,
        CancellationToken ct = default)
    {
        if (skipPreview)
            return Task.FromResult(new WorksheetPreview([], []));

        var clampedFrom = Math.Max(1, from);
        var clampedTo = Math.Min(to, RowsCount);

        List<IReadOnlyDictionary<string, string>> rows;

        if (clampedFrom > clampedTo)
        {
            rows = [];
        }
        else
        {
            var count = clampedTo - clampedFrom + 1;
            rows = new List<IReadOnlyDictionary<string, string>>(count);
            for (var i = clampedFrom; i <= clampedTo; i++)
            {
                ct.ThrowIfCancellationRequested();
                rows.Add(GetRowContent(i));
            }
        }

        return Task.FromResult(new WorksheetPreview(Headers, rows));
    }
}
