namespace SlideGenerator.Services.Scanning.Models.Sheets.Responses;

public record WorkbookSummary(string FilePath, string Name, IReadOnlyList<WorksheetSummary> Worksheets);