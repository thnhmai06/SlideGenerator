using SlideGenerator.Domain.Slide.Entities.Slide;

namespace SlideGenerator.Domain.Slide.Entities.Shape;

public interface IShape : IReadOnlyShape
{
    new ISlide Slide { get; }
    IReadOnlySlide IReadOnlyShape.Slide => Slide;
}