using SlideGenerator.Domain.Slide.Entities.Slide;
using SlideGenerator.Domain.Slide.Rules;

namespace SlideGenerator.Domain.Slide.Entities.Presentation;

public interface IPresentation : IReadOnlyPresentation
{
    new IEnumerable<ISlide> EnumerateSlides();
    IEnumerable<IReadOnlySlide> IReadOnlyPresentation.EnumerateSlides() => EnumerateSlides();
    
    ISlide CopySlide(int from, int to);
    bool RemoveSlide(int index);
    void Save(PresentationExtension? extension = null);
}