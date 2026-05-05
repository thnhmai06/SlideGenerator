using SlideGenerator.Document.Sheet.Models;

namespace SlideGenerator.Pipeline.Scanning.Models.Sheets.Responses;

public sealed record WorksheetSummary(SheetIdentifier Identifier, int Count, WorksheetPreview? Preview = null);