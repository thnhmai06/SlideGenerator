namespace SlideGenerator.Application.Services.Scanning.Models.Sheets;

/// <summary>
///     Worksheet scan information.
/// </summary>
/// <param name="Name">Worksheet name.</param>
/// <param name="Headers">Non-empty worksheet headers.</param>
/// <param name="Count">Number of rows available for generation.</param>
public sealed record WorksheetSummary(
    string Name,
    IReadOnlyList<string> Headers,
    int Count);