namespace SlideGenerator.Application.Services.Scanning.Models.Sheets;

/// <summary>
///     Workbook scan result.
/// </summary>
/// <param name="FilePath">Scanned workbook file path.</param>
/// <param name="Name">The display name of the workbook.</param>
/// <param name="Worksheets">Scanned worksheet metadata.</param>
public record WorkbookSummary(string FilePath, string Name, IReadOnlyList<WorksheetSummary> Worksheets);