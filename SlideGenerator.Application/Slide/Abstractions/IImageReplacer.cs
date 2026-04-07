using SlideGenerator.Domain.Slide.Entities.Shape;

namespace SlideGenerator.Application.Slide.Abstractions;

public interface IImageReplacer
{
    byte[]? Scan(IReadOnlyShape sample);
    int Replace(IShape sample, Stream imageStream);
}