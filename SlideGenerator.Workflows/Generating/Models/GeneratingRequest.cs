using SlideGenerator.Sheets.Models;
using SlideGenerator.Slides.Rules;

namespace SlideGenerator.Workflows.Generating.Models;

public sealed record WorksheetMapping(WorkbookIdentifier Workbook, string WorksheetName);
public sealed record SlideMapping(string PresentationFilePath, int SlideIndex);

public sealed record GeneratingRequest(
    IReadOnlyDictionary<WorksheetMapping, SlideMapping> Graph,
    IReadOnlyList<Texts.GeneralInstruction> TextInstructions,
    IReadOnlyList<GeneralInstruction> ImageInstructions,
    PresentationExtension OutputExtension,
    string SaveFolder)
{
    public IReadOnlyDictionary<WorksheetMapping, SlideMapping> Graph { get; init; } = Graph.Count == 0
        ? throw new ArgumentException("Graph cannot be empty.", nameof(Graph))
        : Graph;

    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;
}