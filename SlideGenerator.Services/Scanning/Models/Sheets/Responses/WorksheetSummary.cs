using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Scanning.Models.Sheets.Responses;

public sealed record WorksheetSummary(SheetIdentifier Identifier, int Count, WorksheetPreview? Preview = null);