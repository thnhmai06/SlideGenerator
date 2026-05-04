using SlideGenerator.Pipelines.Generating.Models.Identifiers;
using SlideGenerator.Pipelines.Generating.Models.Instructions;

namespace SlideGenerator.Pipelines.Generating.Models;

/// <summary>
///     Represents a single mapping node that links multiple source sheets to a target slide template.
///     Contains specific instructions for text and image replacements for this mapping.
/// </summary>
/// <param name="Sheets">The set of source Excel worksheets.</param>
/// <param name="Slide">The target PowerPoint slide template.</param>
/// <param name="TextInstructions">The list of rules for mapping Excel columns to text placeholders.</param>
/// <param name="ImageInstructions">The list of rules for mapping Excel columns to image shapes.</param>
public record MapNode(
    IReadOnlySet<SheetIdentifier> Sheets,
    SlideIdentifier Slide,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions);

/// <summary>
///     Represents the complete configuration for a generation job.
///     A recipe consists of multiple mapping nodes that define how various data sources are merged into slides.
/// </summary>
/// <param name="Nodes">The list of mapping nodes that form the recipe.</param>
public record Recipe(IReadOnlyList<MapNode> Nodes);
