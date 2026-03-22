namespace SlideGenerator.Domain.Slide.Entities;

public interface ISlide : IObject
{
    uint Id { get; }
    
    int Index { get; }

    IEnumerable<IImageShape> EnumerateImageShapes();
}