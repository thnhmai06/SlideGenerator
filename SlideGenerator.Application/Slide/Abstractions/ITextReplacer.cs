using SlideGenerator.Domain.Slide.Entities.Shape;

namespace SlideGenerator.Application.Slide.Abstractions;

public interface ITextReplacer
{
    IEnumerable<string> Scan(IReadOnlyShape sample);

    int Replace(IShape sample, IReadOnlyDictionary<string, string> instructions);
}