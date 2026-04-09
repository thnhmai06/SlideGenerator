using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Domain.Slides.Entities.Presentation;

public interface IPresentation : IReadOnlyPresentation
{
    new IEnumerable<ISlide> EnumerateSlides();
    IEnumerable<IReadOnlySlide> IReadOnlyPresentation.EnumerateSlides() => EnumerateSlides();
    
    ISlide CopySlide(int from, int to);
    bool RemoveSlide(int index);
    void Save(PresentationExtension? extension = null);
}