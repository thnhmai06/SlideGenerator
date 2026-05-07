namespace SlideGenerator.Application.Scanning.Models.Sheets;

/// <summary>
///     Represents scan result for a single worksheet.
/// </summary>
/// <param name="SheetName">Worksheet name.</param>
/// <param name="Headers">Detected header values.</param>
/// <param name="RecordCount">Detected data row count.</param>
public sealed record WorksheetInfo(string SheetName, IReadOnlyList<string> Headers, int RecordCount);