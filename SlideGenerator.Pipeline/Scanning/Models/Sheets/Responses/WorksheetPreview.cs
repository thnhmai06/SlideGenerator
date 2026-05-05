namespace SlideGenerator.Pipeline.Scanning.Models.Sheets.Responses;

public record WorksheetPreview(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);