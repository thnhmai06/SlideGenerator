using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Domain.Slide.Interfaces;

public interface ITemplatePresentation
{
    string FilePath { get; }
    Dictionary<uint, ShapeImagePreview> GetAllImageShapes();
}