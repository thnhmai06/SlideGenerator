namespace SlideGenerator.Pipeline.Scanning.Models.Sheets.Responses;

public record WorkbookSummary(string FilePath, string Name, IReadOnlyList<WorksheetSummary> Worksheets);