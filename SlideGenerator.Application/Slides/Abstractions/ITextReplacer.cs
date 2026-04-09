using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Application.Slides.Abstractions;

public interface ITextReplacer
{
    IEnumerable<string> Scan(IReadOnlyShape sample);

    int Replace(IShape sample, IReadOnlyDictionary<string, string> instructions);
}