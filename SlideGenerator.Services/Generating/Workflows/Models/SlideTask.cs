using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Generating.Workflows.Models;

/// <summary>
/// Represents the data required to fill a specific slide corresponding to a row in a sheet.
/// Uses composition to hold both text and image replacement instructions.
/// </summary>
public sealed class SlideTask(SheetTask sheetTask, int rowIndex)
{
    /// <summary>Gets the parent worksheet task.</summary>
    public SheetTask SheetTask { get; } = sheetTask;

    /// <summary>Gets the 1-based row index in the sheet that this slide represents.</summary>
    public int RowIndex { get; } = rowIndex;

    /// <summary>
    /// Gets the text replacements where Key is the Placeholder text (Mustache tag) and Value is the string replacement.
    /// </summary>
    public Dictionary<string, string> TextReplacements { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the image replacements where Key is the ShapeIdentifier and Value is the ImageTask responsible for processing it.
    /// </summary>
    public Dictionary<ShapeIdentifier, ImageTask> ImageReplacements { get; } = new();
}
