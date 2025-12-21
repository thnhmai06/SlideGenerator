using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Domain.Slide;

/// <summary>
///     Represents a template presentation.
/// </summary>
public interface ITemplatePresentation
{
    string FilePath { get; }
    int SlideCount { get; }
    Dictionary<uint, ImagePreview> GetAllImageShapes();
    IReadOnlyList<ShapeInfo> GetAllShapes();
    IReadOnlyCollection<string> GetAllTextPlaceholders();
}
