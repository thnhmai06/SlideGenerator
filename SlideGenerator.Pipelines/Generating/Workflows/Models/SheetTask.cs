using SlideGenerator.Documents.Sheets.Models;
using SlideGenerator.Documents.Slides.Models;
using SlideGenerator.Pipelines.Generating.Models;

namespace SlideGenerator.Pipelines.Generating.Workflows.Models;

/// <summary>
///     Represents a worksheet that has been validated and assigned an output path.
/// </summary>
public sealed class SheetTask(
    SheetIdentifier identifier,
    SlideIdentifier templateSlide,
    MapNode mapNode,
    PresentationIdentifier outputIdentifier)
{
    /// <summary>Gets the unique identifier for the source worksheet.</summary>
    public SheetIdentifier Identifier { get; } = identifier;

    /// <summary>Gets the identifier for the slide to be used as a template for this sheet.</summary>
    public SlideIdentifier TemplateSlide { get; } = templateSlide;

    /// <summary>Gets the mapping configuration node associated with this sheet.</summary>
    public MapNode MapNode { get; } = mapNode;

    /// <summary>Gets the final output identifier for the generated presentation corresponding to this sheet.</summary>
    public PresentationIdentifier OutputIdentifier { get; } = outputIdentifier;
}