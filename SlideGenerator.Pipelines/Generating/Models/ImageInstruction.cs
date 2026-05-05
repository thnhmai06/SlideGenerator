using SlideGenerator.Documents.Sheets.Models;
using SlideGenerator.Documents.Slides.Models;

namespace SlideGenerator.Pipelines.Generating.Models;

/// <summary>
///     Defines a mapping between one or more Excel columns and one or more image shapes in a slide.
///     Includes rules for image processing and fallback behavior.
/// </summary>
/// <param name="Shapes">The set of target PowerPoint shapes to be filled with images.</param>
/// <param name="Columns">The list of Excel columns providing image URIs or paths.</param>
/// <param name="EditOptions">The processing rules (ROI, crop, resize) for the images.</param>
/// <param name="FallbackImagePath">Optional path to a default image if the source is missing or invalid.</param>
public record ImageInstruction(
    IReadOnlySet<ShapeIdentifier> Shapes,
    IReadOnlyList<ColumnIdentifier> Columns,
    EditOptions EditOptions,
    string? FallbackImagePath = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the fallback image.
    /// </summary>
    public string? FallbackImagePath
    {
        get;
        init => field = string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
    } = FallbackImagePath;
}
