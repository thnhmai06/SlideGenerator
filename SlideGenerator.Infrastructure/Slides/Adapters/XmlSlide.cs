/* LEGACY-OPENXML — replaced by SfSlide (Syncfusion.Presentation.NET)
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Domain.Slides.Entities.Slide;

namespace SlideGenerator.Infrastructure.Slides.Adapters;

/// <summary>
///     Represents a slide within an Open XML presentation.
/// </summary>
public class XmlSlide : ISlide
{
    private readonly XmlPresentation _xmlPresentation;
    internal readonly SlidePart Core;

    internal XmlSlide(XmlPresentation presentation, SlidePart core)
    {
        _xmlPresentation = presentation;
        Core = core;
    }

    public required int Index { get; init; }
    public IPresentation Presentation => _xmlPresentation;
    public required uint Id { get; init; }
    public string? Name => Core.Slide?.CommonSlideData?.Name?.ToString();

    public IEnumerable<IShape> DescendShapes()
    {
        var shapeTree = Core.Slide?.CommonSlideData?.ShapeTree;
        if (shapeTree == null)
            return [];

        return shapeTree.Descendants<OpenXmlCompositeElement>()
            .Select(e => new XmlShape(this, e));
    }
}
*/
