using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Domain.Slides.Entities.Slide;

public interface IReadOnlySlide
{
    uint Id { get; }
    string? Name { get; }
    IReadOnlyPresentation Presentation { get; }
    IEnumerable<IReadOnlyShape> DescendShapes();
}