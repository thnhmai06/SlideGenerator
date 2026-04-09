using SlideGenerator.Domain.Slides.Entities.Slide;

namespace SlideGenerator.Domain.Slides.Entities.Shape;

public interface IShape : IReadOnlyShape
{
    new ISlide Slide { get; }
    IReadOnlySlide IReadOnlyShape.Slide => Slide;
}