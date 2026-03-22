using SlideGenerator.Domain.Slide.Rules;

namespace SlideGenerator.Domain.Slide.Entities;

public interface IPresentation: IObject
{
    string FilePath { get; }
    IEnumerable<ISlide> EnumerateSlides();
    void Save(PresentationExtension? extension = null);
}