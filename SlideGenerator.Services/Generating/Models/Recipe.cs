using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Models.Instructions;

namespace SlideGenerator.Services.Generating.Models;

public record MapNode(
    IReadOnlySet<SheetIdentifier> Sheets,
    SlideIdentifier Slide,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions);

public record Recipe(IReadOnlyList<MapNode> Nodes);
