using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Sheets.Models.Previews;

namespace SlideGenerator.Domain.Sheets.Entities;

/// <summary>
///     Represents a read-only view of a single worksheet within a workbook.
/// </summary>
public interface IReadOnlyWorksheet
{
    /// <summary>Gets the parent workbook that contains this worksheet.</summary>
    IReadOnlyWorkbook Workbook { get; }

    /// <summary>Gets the unique identifier for this worksheet.</summary>
    WorksheetIdentifier Identifier { get; }

    /// <summary>Gets the name of the worksheet.</summary>
    string Name => Identifier.Name;

    /// <summary>Gets a list of column headers parsed from the first row of the worksheet.</summary>
    IReadOnlyList<string> Headers { get; }

    /// <summary>Gets the total number of data rows in the worksheet (excluding the header row).</summary>
    int RowsCount { get; }

    /// <summary>
    ///     Retrieves the cell contents for a specific row, mapped by column headers.
    /// </summary>
    /// <param name="rowIndex">The 1-based index of the data row (row 1 is the first row after headers).</param>
    /// <returns>A read-only dictionary mapping column headers to their corresponding string values.</returns>
    IReadOnlyDictionary<string, string> GetRowContent(int rowIndex);

    /// <summary>
    ///     Returns a <see cref="WorksheetPreview" /> containing headers and a slice of data rows.
    /// </summary>
    /// <param name="from">1-based index of the first data row to include (default: 1).</param>
    /// <param name="to">1-based index of the last data row to include (default: 10).</param>
    /// <param name="skipPreview">When <see langword="true" />, returns an empty preview immediately without reading data.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<WorksheetPreview> GetPreview(int from = 1, int to = 10, bool skipPreview = false, CancellationToken ct = default);
}