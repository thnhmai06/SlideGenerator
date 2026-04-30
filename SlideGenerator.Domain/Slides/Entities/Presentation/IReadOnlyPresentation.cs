using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Domain.Slides.Entities.Presentation;

/// <summary>
///     Represents a read-only view of a presentation document.
/// </summary>
public interface IReadOnlyPresentation
{
    /// <summary>Gets the unique identifier of the presentation.</summary>
    PresentationIdentifier Identifier { get; }

    /// <summary>Gets the absolute or relative file path to the presentation file.</summary>
    string FilePath => Identifier.FilePath;

    /// <summary>
    ///     Lists all slides within the presentation as read-only objects.
    /// </summary>
    /// <returns>A collection of <see cref="IReadOnlySlide" /> instances.</returns>
    IEnumerable<IReadOnlySlide> EnumerateSlides();
}