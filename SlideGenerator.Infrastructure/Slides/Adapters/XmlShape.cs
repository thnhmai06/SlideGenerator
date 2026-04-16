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

public class XmlShape : IShape
{
    private readonly XmlSlide _xmlSlide;
    internal readonly OpenXmlCompositeElement Core;

    internal XmlShape(XmlSlide slide, OpenXmlCompositeElement element)
    {
        _xmlSlide = slide;
        Core = element;
    }

    public ISlide Slide => _xmlSlide;

    public uint Id => Core switch
    {
        Picture picture => picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Id?.Value,
        Shape shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value,
        _ => null
    } ?? uint.MaxValue;

    public string Name => Core switch
    {
        Picture picture => picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.ToString(),
        Shape shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.ToString(),
        _ => null
    } ?? string.Empty;

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

    public string? TextContent => Core switch
    {
        Shape shape => shape.TextBody?.InnerText,
        _ => null
    };

    public bool IsPicture => Core is Picture;

    public bool HasBlipFill => Core is Shape shape &&
                               shape.ShapeProperties?.GetFirstChild<BlipFill>()?.Blip?.Embed?.HasValue == true;

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