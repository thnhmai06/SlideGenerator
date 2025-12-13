using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Domain.Slide.Interfaces;

public interface ITemplatePresentation : IDisposable
{
    string FilePath { get; }
    Dictionary<uint, ShapeImageData> GetAllImageShapes();
}