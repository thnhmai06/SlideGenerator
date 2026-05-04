using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Generating.Workflows.Models;

/// <summary>
/// Represents a worksheet that has been validated and assigned an output path.
/// </summary> 
public sealed class SheetTask(
    SheetIdentifier identifier,
    SlideIdentifier templateSlide,
    MapNode mapNode,
    string outputPath)
{
    public SheetIdentifier Identifier { get; } = identifier;
    public SlideIdentifier TemplateSlide { get; } = templateSlide;
    public MapNode MapNode { get; } = mapNode;
    public string OutputPath { get; } = outputPath;
}