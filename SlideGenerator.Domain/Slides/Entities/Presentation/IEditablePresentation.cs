using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Domain.Slides.Entities.Presentation;

public interface IPresentation : IReadOnlyPresentation
{
    IEnumerable<IReadOnlySlide> IReadOnlyPresentation.EnumerateSlides()
    {
        return EnumerateSlides();
    }

    new IEnumerable<ISlide> EnumerateSlides();

    ISlide CopySlide(int from, int to);
    bool RemoveSlide(int index);
    void Save(PresentationExtension? extension = null);
}