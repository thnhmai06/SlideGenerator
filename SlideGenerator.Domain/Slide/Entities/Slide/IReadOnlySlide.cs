using SlideGenerator.Domain.Slide.Entities.Presentation;
using SlideGenerator.Domain.Slide.Entities.Shape;

namespace SlideGenerator.Domain.Slide.Entities.Slide;

public interface IReadOnlySlide
{
    uint Id { get; }
    string? Name { get; }
    IReadOnlyPresentation Presentation { get; }
    IEnumerable<IReadOnlyShape> DescendShapes();
}