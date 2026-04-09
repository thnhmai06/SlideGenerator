using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Domain.Slides.Entities.Slide;

public interface ISlide : IReadOnlySlide
{
    new IPresentation Presentation { get; }
    IReadOnlyPresentation IReadOnlySlide.Presentation => Presentation;
    
    new IEnumerable<IShape> DescendShapes();
    IEnumerable<IReadOnlyShape> IReadOnlySlide.DescendShapes() => DescendShapes();
}