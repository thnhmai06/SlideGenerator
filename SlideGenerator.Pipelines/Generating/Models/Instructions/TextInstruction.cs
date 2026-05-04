using SlideGenerator.Pipelines.Generating.Models.Identifiers;

namespace SlideGenerator.Pipelines.Generating.Models.Instructions;

/// <summary>
///     Defines a mapping between one or more Excel columns and one or more text placeholders in a slide.
/// </summary>
/// <param name="Placeholders">The set of placeholder tags (e.g., "{{Name}}") to be replaced.</param>
/// <param name="Columns">The list of Excel columns whose values will provide the replacement text.</param>
public record TextInstruction(
    IReadOnlySet<string> Placeholders,
    IReadOnlyList<ColumnIdentifier> Columns);