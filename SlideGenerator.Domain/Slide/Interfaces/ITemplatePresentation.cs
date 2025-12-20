using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Domain.Slide.Interfaces;

public interface ITemplatePresentation
{
    string FilePath { get; }

    int SlideCount { get; }
    Dictionary<uint, ShapeImagePreview> GetAllImageShapes();
}