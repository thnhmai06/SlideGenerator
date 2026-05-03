using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Generating.Models.Instructions;

public record ImageInstruction(
    IReadOnlySet<ShapeIdentifier> Shapes,
    IReadOnlyList<string> ColumnNames,
    EditOptions EditOptions,
    string? FallbackImagePath = null);