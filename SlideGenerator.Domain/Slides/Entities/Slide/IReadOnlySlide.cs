using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Domain.Slides.Entities.Slide;

/// <summary>
///     Represents a read-only view of a slide within a presentation.
/// </summary>
public interface IReadOnlySlide
{
    /// <summary>Gets the unique ID of the slide.</summary>
    uint Id { get; }

    /// <summary>Gets the name of the slide, if any.</summary>
    string? Name { get; }

    /// <summary>Gets the parent presentation containing this slide.</summary>
    IReadOnlyPresentation Presentation { get; }

    /// <summary>
    ///     Recursively enumerates all read-only shapes within the slide.
    /// </summary>
    /// <returns>A collection of <see cref="IReadOnlyShape" /> instances.</returns>
    IEnumerable<IReadOnlyShape> DescendShapes();
}
