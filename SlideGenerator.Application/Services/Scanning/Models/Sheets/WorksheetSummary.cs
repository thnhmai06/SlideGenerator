using SlideGenerator.Domain.Sheets.Models.Previews;

namespace SlideGenerator.Application.Services.Scanning.Models.Sheets;

/// <summary>
///     Worksheet scan information.
/// </summary>
/// <param name="Name">Worksheet name.</param>
/// <param name="Preview">Header + up-to-10-row data preview captured during scanning.</param>
/// <param name="Count">Number of data rows available for generation.</param>
public sealed record WorksheetSummary(
    string Name,
    WorksheetPreview Preview,
    int Count);
