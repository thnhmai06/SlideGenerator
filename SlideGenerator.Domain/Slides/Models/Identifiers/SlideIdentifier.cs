namespace SlideGenerator.Domain.Slides.Models.Identifiers;

/// <summary>
///     Identifies a specific slide within a presentation.
/// </summary>
/// <param name="Presentation">The identifier of the parent presentation.</param>
/// <param name="Index">The 1-based index of the slide within the presentation.</param>
public sealed record SlideIdentifier(PresentationIdentifier Presentation, int Index)
{
    /// <summary>Gets the 1-based index of the slide.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is less than 1.</exception>
    public int Index { get; init; } = Index > 0
        ? Index
        : throw new ArgumentOutOfRangeException(nameof(Index), "Slide index must be greater than 0.");

    /// <summary>
    ///     Creates a child <see cref="ShapeIdentifier" /> for the specified shape ID.
    /// </summary>
    /// <param name="id">The unique ID of the shape.</param>
    /// <returns>A new <see cref="ShapeIdentifier" /> linked to this slide.</returns>
    public ShapeIdentifier GetShape(uint id)
    {
        return new ShapeIdentifier(this, id);
    }
}
