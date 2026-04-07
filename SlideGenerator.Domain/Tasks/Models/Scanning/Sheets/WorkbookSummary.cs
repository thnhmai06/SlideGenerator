namespace SlideGenerator.Domain.Tasks.Models.Scanning.Sheets;

/// <summary>
///     Workbook scan result.
/// </summary>
/// <param name="FilePath">Scanned workbook file path.</param>
/// <param name="Worksheets">Scanned worksheet metadata.</param>
public record WorkbookSummary(string FilePath, string Name, IReadOnlyList<WorksheetSummary> Worksheets);

