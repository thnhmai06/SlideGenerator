using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Scanning.Models.Sheets.Requests;

public sealed record BookSummaryRequest(BookIdentifier Identifier, bool GetPreview = true);