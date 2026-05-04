namespace SlideGenerator.Services.Generating.Models.Identifiers;

/// <summary>
///     Uniquely identifies a specific slide within a PowerPoint presentation.
/// </summary>
/// <param name="PresentationPath">The path to the presentation.</param>
/// <param name="SlideIndex">The 1-based index of the slide.</param>
/// <param name="PresentationPassword">Optional password for the presentation.</param>
public record SlideIdentifier(string PresentationPath, int SlideIndex, string? PresentationPassword = null)
    : PresentationIdentifier(PresentationPath, PresentationPassword)
{
    /// <summary>
    ///     Gets the 1-based index of the slide. Guaranteed to be at least 1.
    /// </summary>
    public int SlideIndex { get; init; } = Math.Max(1, SlideIndex);
}
