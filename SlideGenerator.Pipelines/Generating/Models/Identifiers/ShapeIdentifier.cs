namespace SlideGenerator.Pipelines.Generating.Models.Identifiers;

/// <summary>
///     Uniquely identifies a specific shape within a PowerPoint slide.
/// </summary>
/// <param name="PresentationPath">The path to the presentation.</param>
/// <param name="SlideIndex">The 1-based index of the slide.</param>
/// <param name="ShapeName">The unique name of the shape (e.g., "Rectangle 1").</param>
/// <param name="PresentationPassword">Optional password for the presentation.</param>
public record ShapeIdentifier(string PresentationPath, int SlideIndex, string ShapeName, string? PresentationPassword = null)
    : SlideIdentifier(PresentationPath, SlideIndex, PresentationPassword);
