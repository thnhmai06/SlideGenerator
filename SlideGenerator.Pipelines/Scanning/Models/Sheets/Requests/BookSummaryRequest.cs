using SlideGenerator.Documents.Sheets.Models;

namespace SlideGenerator.Pipelines.Scanning.Models.Sheets.Requests;

public sealed record BookSummaryRequest(BookIdentifier Identifier, bool GetPreview = true);
