using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Scanning.Models;

/// <summary>
///     Input data for a scanning process.
/// </summary>
public sealed record ScanningRequest(
    IReadOnlyList<WorkbookIdentifier> Workbooks,
    IReadOnlyList<PresentationIdentifier> Presentations);