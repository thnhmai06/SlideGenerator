namespace SlideGenerator.Workflows.Scanning.Models;

public record WorksheetPreview(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

public sealed record WorksheetSummary(
    string Name,
    WorksheetPreview Preview,
    int Count);

public record WorkbookSummary(string FilePath, string Name, IReadOnlyList<WorksheetSummary> Worksheets);

public sealed record SlidePreview(int Index, byte[] Image);

public record ShapePreview(uint Id, string Name, System.Drawing.RectangleF Bounds, byte[] Image);

public sealed record SlideSummary(
    int Index,
    uint Id,
    string Name,
    SlidePreview Preview,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<ShapePreview> ImageShapes);

public record PresentationSummary(string FilePath, IReadOnlyList<SlideSummary> Slides);