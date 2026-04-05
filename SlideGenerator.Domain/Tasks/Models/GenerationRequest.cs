using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Models;
using SlideGenerator.Domain.Slide.Rules;

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
public sealed record GenerationRequest(
    IReadOnlyDictionary<WorksheetIdentifier, SlideIdentifier> Graph,
    IReadOnlyList<Text.GeneralInstruction> TextInstructions,
    IReadOnlyList<Image.GeneralInstruction> ImageInstructions,
    PresentationExtension OutputExtension,
    string SaveFolder)
{
    public const string Name = nameof(GenerationRequest);
    public const string Description = "A generating request payload";

    public IReadOnlyDictionary<WorksheetIdentifier, SlideIdentifier> Graph { get; init; } = Graph.Count == 0
        ? throw new ArgumentException("Graph cannot be empty.", nameof(Graph))
        : Graph;

    public PresentationExtension OutputExtension { get; init; } =
        PresentationExtensions.OutputExtensions.Contains(OutputExtension)
            ? OutputExtension
            : throw new ArgumentException(
                $"Output extension must be one of the following: {string.Join(", ", PresentationExtensions.OutputExtensions)}.",
                nameof(OutputExtension));

    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;
}