using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Domain.Slide.Entities.Presentation;
using SlideGenerator.Domain.Slide.Entities.Shape;
using SlideGenerator.Domain.Slide.Entities.Slide;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class XmlSlide : ISlide
{
    internal readonly SlidePart Core;

    internal XmlSlide(XmlPresentation presentation, SlidePart core)
    {
        _xmlPresentation = presentation;
        Core = core;
    }

    private readonly XmlPresentation _xmlPresentation;

    public IPresentation Presentation => _xmlPresentation;
    public required int Index { get; init; }
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