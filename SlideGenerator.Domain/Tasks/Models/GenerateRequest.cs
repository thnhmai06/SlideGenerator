using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Represents a generation request payload consumed by endpoint.
/// </summary>
/// <param name="Graph">Sheet-to-template-slide mapping.</param>
/// <param name="TextInstructions">Text replacement bindings.</param>
/// <param name="ImageInstructions">Image replacement bindings.</param>
/// <param name="SaveFolder">Save folder for generated presentations.</param>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 00:58:42 GMT+7
/// </remarks>
public sealed record GenerateRequest(
    IReadOnlyDictionary<WorksheetIdentifier, SlideIdentifier> Graph,
    IReadOnlyList<Text.GeneralInstruction> TextInstructions,
    IReadOnlyList<Image.GeneralInstruction> ImageInstructions,
    string SaveFolder)
{
    public IReadOnlyDictionary<WorksheetIdentifier, SlideIdentifier> Graph { get; init; } = Graph.Count == 0
        ? throw new ArgumentException("Graph cannot be empty.", nameof(Graph))
        : Graph;
    
    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;
}