using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Models.Instructions;

namespace SlideGenerator.Services.Generating.Models;

public record Recipe(IReadOnlyDictionary<SheetIdentifier, SlideIdentifier> Graph,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions)
{
    public IReadOnlyDictionary<SheetIdentifier, SlideIdentifier> Graph { get; init; } = Graph.Count == 0
        ? throw new ArgumentException("Graph cannot be empty.", nameof(Graph))
        : Graph;
}
