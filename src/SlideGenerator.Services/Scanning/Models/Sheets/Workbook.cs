namespace SlideGenerator.Services.Scanning.Models.Sheets;

/// <summary>
///     Represents worksheet scan response payload.
/// </summary>
/// <param name="FilePath">Scanned spreadsheet file path.</param>
/// <param name="Sheets">Collection of worksheet scan items.</param>
public sealed record Workbook(string FilePath, IReadOnlyList<Worksheet> Sheets);