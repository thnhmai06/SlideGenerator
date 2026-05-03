namespace SlideGenerator.Services.Scanning.Models.Sheets.Responses;

public sealed record WorksheetSummary(string Name, int Count, WorksheetPreview? Preview = null);