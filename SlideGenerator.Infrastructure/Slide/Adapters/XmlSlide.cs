using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using SlideGenerator.Domain.Slide.Entities;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class XmlSlide : ISlide
{
    internal readonly SlidePart Core;

    internal XmlSlide(SlidePart core)
    {
        Core = core;
    }
    
    public required XmlPresentation Parent { get; init; }
    public required int Index { get; init; }
    public required uint Id { get; init; }

    public IEnumerable<IImageShape> EnumerateImageShapes()
    {
        var slide = Core.Slide;
        if (slide == null) yield break;

        var pictures = slide.Descendants<Picture>();
        var shapes = slide.Descendants<Shape>()
            .Where(s => s.ShapeProperties?.GetFirstChild<BlipFill>()?.Blip?.Embed != null);

        foreach (var pic in pictures)
            yield return new XmlImageShape(pic) { Parent = this };
        foreach (var shape in shapes)
            yield return new XmlImageShape(shape) { Parent = this };
    }
}