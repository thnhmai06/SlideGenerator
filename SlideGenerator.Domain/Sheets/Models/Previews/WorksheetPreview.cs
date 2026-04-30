namespace SlideGenerator.Domain.Sheets.Models.Previews;

/// <summary>
///     Represents a lightweight preview of a worksheet's data — headers plus a bounded set of rows.
///     Each row is a dictionary mapping column header to display text.
/// </summary>
/// <param name="Headers">The column headers parsed from the first row.</param>
/// <param name="Rows">
///     A bounded list of data rows. Each element maps header → display text for one row.
///     Empty when preview was skipped (<c>skipPreview = true</c>).
/// </param>
public sealed record WorksheetPreview(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);
