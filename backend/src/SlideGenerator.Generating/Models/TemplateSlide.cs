namespace SlideGenerator.Generating.Models;

/// <summary>
///     Represents a template source used for slide generation.
/// </summary>
/// <param name="FilePath">Absolute or relative path to template presentation file.</param>
/// <param name="Index">1-based slide index in template presentation.</param>
public sealed record TemplateSlide(string FilePath, int Index);