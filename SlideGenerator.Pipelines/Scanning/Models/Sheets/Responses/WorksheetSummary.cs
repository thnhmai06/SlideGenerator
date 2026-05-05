using SlideGenerator.Documents.Sheets.Models;

namespace SlideGenerator.Pipelines.Scanning.Models.Sheets.Responses;

public sealed record WorksheetSummary(SheetIdentifier Identifier, int Count, WorksheetPreview? Preview = null);