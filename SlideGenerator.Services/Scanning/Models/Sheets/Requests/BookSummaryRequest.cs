namespace SlideGenerator.Services.Scanning.Models.Sheets.Requests;

public sealed record BookSummaryRequest(string WorkbookPath, bool GetPreview = true);