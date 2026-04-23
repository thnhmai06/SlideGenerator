using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Domain.Slides.Entities.Slide;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using BlipFill = DocumentFormat.OpenXml.Drawing.BlipFill;

namespace SlideGenerator.Infrastructure.Slides.Adapters;

/// <summary>
///     Represents a shape within an Open XML slide.
/// </summary>
public class XmlShape : IShape
{
    /// <summary>
    ///     The parent slide containing this shape.
    /// </summary>
    private readonly XmlSlide _xmlSlide;

    /// <summary>
    ///     The underlying Open XML element representing the shape.
    /// </summary>
    internal readonly OpenXmlCompositeElement Core;

    /// <summary>
    ///     Initializes a new instance of the <see cref="XmlShape" /> class.
    /// </summary>
    /// <param name="slide">The parent slide.</param>
    /// <param name="element">The Open XML element.</param>
    internal XmlShape(XmlSlide slide, OpenXmlCompositeElement element)
    {
        _xmlSlide = slide;
        Core = element;
    }

    /// <summary>
    ///     Gets the slide that contains this shape.
    /// </summary>
    public ISlide Slide => _xmlSlide;

    /// <summary>
    ///     Gets the unique identifier of the shape.
    /// </summary>
    public uint Id => Core switch
    {
        Picture picture => picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Id?.Value,
        Shape shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value,
        _ => null
    } ?? uint.MaxValue;

    /// <summary>
    ///     Gets the name of the shape.
    /// </summary>
    public string Name => Core switch
    {
        Picture picture => picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.ToString(),
        Shape shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.ToString(),
        _ => null
    } ?? string.Empty;

    /// <summary>
    ///     Gets the bounding box of the shape in pixels.
    /// </summary>
    public RectangleF Bounds
    {
        get
        {
            var transform = ((dynamic)Core).ShapeProperties?.Transform2D;

            var x = (float)(transform?.Offset?.X?.Value ?? 0) / Utilities.EmuPerPixel;
            var y = (float)(transform?.Offset?.Y?.Value ?? 0) / Utilities.EmuPerPixel;
            var w = (float)(transform?.Extents?.Cx?.Value ?? 0) / Utilities.EmuPerPixel;
            var h = (float)(transform?.Extents?.Cy?.Value ?? 0) / Utilities.EmuPerPixel;
            return new RectangleF(x, y, w, h);
        }
    }

    /// <summary>
    ///     Gets the text content of the shape, if applicable.
    /// </summary>
    public string? TextContent => Core switch
    {
        Shape shape => shape.TextBody?.InnerText,
        _ => null
    };

    /// <summary>
    ///     Gets a value indicating whether the shape is a picture.
    /// </summary>
    public bool IsPicture => Core is Picture;

    /// <summary>
    ///     Gets a value indicating whether the shape has a blip fill.
    /// </summary>
    public bool HasBlipFill => Core is Shape shape &&
                               shape.ShapeProperties?.GetFirstChild<BlipFill>()?.Blip?.Embed?.HasValue == true;

    /// <summary>
    ///     Attempts to get the picture data if the shape is a picture.
    /// </summary>
    /// <param name="image">The resulting image data as a byte array.</param>
    /// <returns><see langword="true" /> if the picture data was successfully retrieved; otherwise, <see langword="false" />.</returns>
    public bool TryGetPicture([MaybeNullWhen(false)] out byte[] image)
    {
        if (!IsPicture)
        {
            image = null;
            return false;
        }

        var picture = (Picture)Core;
        var relId = picture.BlipFill?.Blip?.Embed?.Value;
        return TryGetImagePartByRelId(relId, out image);
    }

    /// <summary>
    ///     Attempts to get the blip fill data if the shape has one.
    /// </summary>
    /// <param name="image">The resulting image data as a byte array.</param>
    /// <returns><see langword="true" /> if the blip fill data was successfully retrieved; otherwise, <see langword="false" />.</returns>
    public bool TryGetBlipFill([MaybeNullWhen(false)] out byte[] image)
    {
        if (!HasBlipFill)
        {
            image = null;
            return false;
        }

        var element = (Shape)Core;
        var blipFill = element.Descendants<BlipFill>().FirstOrDefault();
        var relId = blipFill?.Blip?.Embed?.Value;
        return TryGetImagePartByRelId(relId, out image);
    }

    /// <summary>
    ///     Attempts to retrieve the image data from a relationship ID.
    /// </summary>
    /// <param name="relId">The relationship identifier.</param>
    /// <param name="image">The resulting image data as a byte array.</param>
    /// <returns><see langword="true" /> if the image data was successfully retrieved; otherwise, <see langword="false" />.</returns>
    private bool TryGetImagePartByRelId(string? relId, [MaybeNullWhen(false)] out byte[] image)
    {
        if (string.IsNullOrEmpty(relId) || _xmlSlide.Core.GetPartById(relId) is not ImagePart imagePart)
        {
            image = null;
            return false;
        }

        using var stream = imagePart.GetStream(FileMode.Open, FileAccess.Read);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        image = memoryStream.ToArray();
        return true;
    }
}