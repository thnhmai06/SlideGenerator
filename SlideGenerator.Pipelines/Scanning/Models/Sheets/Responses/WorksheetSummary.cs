using SlideGenerator.Pipelines.Generating.Models.Identifiers;

namespace SlideGenerator.Pipelines.Scanning.Models.Sheets.Responses;

public sealed record WorksheetSummary(SheetIdentifier Identifier, int Count, WorksheetPreview? Preview = null);