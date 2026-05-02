using SlideGenerator.Sheets.Models;

namespace SlideGenerator.Workflows.Scanning.Models;

public sealed record ScanningRequest(
    IReadOnlyList<WorkbookIdentifier> Workbooks,
    IReadOnlyList<string> PresentationFilePaths);