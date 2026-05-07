namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Represents a slide source.
/// </summary>
/// <param name="FilePath">The file path to presentation file contains this slide.</param>
/// <param name="Index">1-based slide index in template presentation.</param>
public sealed record SlideInfo(string FilePath, int Index);