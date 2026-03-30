using SlideGenerator.Domain.Slide.Rules;

namespace SlideGenerator.Domain.Slide.Entities;

public interface IPresentation: ISlideObject
{
    string FilePath { get; }
    IEnumerable<ISlide> EnumerateSlides();
    ISlide CopySlide(int from, int to);
    bool RemoveSlide(int position);
    void Save(PresentationExtension? extension = null);
}