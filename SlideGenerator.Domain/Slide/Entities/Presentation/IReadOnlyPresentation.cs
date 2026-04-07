using SlideGenerator.Domain.Slide.Entities.Slide;
using SlideGenerator.Domain.Slide.Models.Identifiers;

namespace SlideGenerator.Domain.Slide.Entities.Presentation;

public interface IReadOnlyPresentation
{
    PresentationIdentifier Identifier { get; }
    string FilePath => Identifier.FilePath;
    IEnumerable<IReadOnlySlide> EnumerateSlides();
}