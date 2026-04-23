namespace SlideGenerator.Domain.Slides.Models.Identifiers;

/// <summary>
///     Identifies a presentation file.
/// </summary>
/// <param name="FilePath">The absolute or relative file path to the presentation file.</param>
public sealed record PresentationIdentifier(string FilePath)
{
    /// <summary>Gets the file path to the presentation file.</summary>
    /// <exception cref="ArgumentException">Thrown when the provided file path is null or whitespace.</exception>
    public string FilePath { get; init; } = !string.IsNullOrWhiteSpace(FilePath)
        ? FilePath
        : throw new ArgumentException("File path cannot be null or whitespace.", nameof(FilePath));

    /// <summary>
    ///     Creates a child <see cref="SlideIdentifier" /> for the specified slide index.
    /// </summary>
    /// <param name="index">The 1-based index of the slide.</param>
    /// <returns>A new <see cref="SlideIdentifier" /> linked to this presentation.</returns>
    public SlideIdentifier GetSlide(int index)
    {
        return new SlideIdentifier(this, index);
    }
}