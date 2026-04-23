using SlideGenerator.Domain.Slides.Entities.Slide;

namespace SlideGenerator.Domain.Slides.Entities.Shape;

/// <summary>
///     Represents an editable shape within a slide.
/// </summary>
public interface IShape : IReadOnlyShape
{
    /// <summary>Gets the parent editable slide containing this shape.</summary>
    new ISlide Slide { get; }

    /// <inheritdoc />
    IReadOnlySlide IReadOnlyShape.Slide => Slide;
}
