using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Domain.Slides.Entities.Slide;

/// <summary>
///     Represents an editable slide within a presentation.
/// </summary>
public interface ISlide : IReadOnlySlide
{
    /// <summary>Gets the parent editable presentation containing this slide.</summary>
    new IPresentation Presentation { get; }

    /// <inheritdoc />
    IReadOnlyPresentation IReadOnlySlide.Presentation => Presentation;

    /// <inheritdoc />
    IEnumerable<IReadOnlyShape> IReadOnlySlide.DescendShapes()
    {
        return DescendShapes();
    }

    /// <summary>
    ///     Recursively enumerates all editable shapes within the slide.
    /// </summary>
    /// <returns>A collection of <see cref="IShape" /> instances.</returns>
    new IEnumerable<IShape> DescendShapes();
}