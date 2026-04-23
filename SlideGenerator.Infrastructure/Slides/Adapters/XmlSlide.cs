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
    /// <summary>
    ///     The parent presentation containing this slide.
    /// </summary>
    private readonly XmlPresentation _xmlPresentation;

    /// <summary>
    ///     The underlying Open XML <see cref="SlidePart" /> representing the slide.
    /// </summary>
    internal readonly SlidePart Core;

    /// <summary>
    ///     Initializes a new instance of the <see cref="XmlSlide" /> class.
    /// </summary>
    /// <param name="presentation">The parent presentation.</param>
    /// <param name="core">The Open XML slide part.</param>
    internal XmlSlide(XmlPresentation presentation, SlidePart core)
    {
        _xmlPresentation = presentation;
        Core = core;
    }

    /// <summary>
    ///     Gets the 1-based index of the slide in the presentation.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    ///     Gets the presentation that contains this slide.
    /// </summary>
    public IPresentation Presentation => _xmlPresentation;

    /// <summary>
    ///     Gets the unique identifier of the slide.
    /// </summary>
    public required uint Id { get; init; }

    /// <summary>
    ///     Gets the name of the slide.
    /// </summary>
    public string? Name => Core.Slide?.CommonSlideData?.Name?.ToString();

    /// <summary>
    ///     Enumerates all shapes contained within the slide.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="IShape" /> objects.</returns>
    public IEnumerable<IShape> DescendShapes()
    {
        var shapeTree = Core.Slide?.CommonSlideData?.ShapeTree;
        if (shapeTree == null)
            return [];

        return shapeTree.Descendants<OpenXmlCompositeElement>()
            .Select(e => new XmlShape(this, e));
    }
}