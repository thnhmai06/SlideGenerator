namespace SlideGenerator.Domain.Slide.Entities;

public interface ISlide : ISlideObject
{
    uint Id { get; }
    
    int Index { get; }

    IEnumerable<IImageShape> EnumerateImageShapes();
}