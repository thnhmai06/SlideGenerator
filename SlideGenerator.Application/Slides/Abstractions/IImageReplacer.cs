using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Application.Slides.Abstractions;

public interface IImageReplacer
{
    byte[]? Scan(IReadOnlyShape sample);
    int Replace(IShape sample, Stream imageStream);
}