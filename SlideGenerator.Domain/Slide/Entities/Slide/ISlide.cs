using SlideGenerator.Domain.Slide.Entities.Presentation;
using SlideGenerator.Domain.Slide.Entities.Shape;

namespace SlideGenerator.Domain.Slide.Entities.Slide;

public interface ISlide : IReadOnlySlide
{
    new IPresentation Presentation { get; }
    IReadOnlyPresentation IReadOnlySlide.Presentation => Presentation;
    
    new IEnumerable<IShape> DescendShapes();
    IEnumerable<IReadOnlyShape> IReadOnlySlide.DescendShapes() => DescendShapes();
}