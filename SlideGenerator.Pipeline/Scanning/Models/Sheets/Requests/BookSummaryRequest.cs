using SlideGenerator.Document.Sheet.Models;

namespace SlideGenerator.Pipeline.Scanning.Models.Sheets.Requests;

public sealed record BookSummaryRequest(BookIdentifier Identifier, bool GetPreview = true);