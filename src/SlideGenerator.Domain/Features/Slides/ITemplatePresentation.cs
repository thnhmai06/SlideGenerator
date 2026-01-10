using SlideGenerator.Domain.Features.Slides.Components;

namespace SlideGenerator.Domain.Features.Slides;

/// <summary>
///     Represents a template presentation.
/// </summary>
public interface ITemplatePresentation : IDisposable
{
    string FilePath { get; }
    int SlideCount { get; }
    Dictionary<uint, ImagePreview> GetAllImageShapes();
    IReadOnlyList<ShapeInfo> GetAllShapes();
    IReadOnlyCollection<string> GetAllTextPlaceholders();
}