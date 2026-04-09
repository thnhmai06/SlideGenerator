using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Domain.Slides.Entities.Presentation;

public interface IReadOnlyPresentation
{
    PresentationIdentifier Identifier { get; }
    string FilePath => Identifier.FilePath;
    IEnumerable<IReadOnlySlide> EnumerateSlides();
}