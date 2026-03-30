using System.Drawing;
using DocumentFormat.OpenXml;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Rules;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class XmlImageShape : IImageShape
{
    internal readonly OpenXmlCompositeElement Core; // Picture || FilledShape

    public required XmlSlide Parent { get; init; }

    internal XmlImageShape(Picture picture)
    {
        Core = picture;
        Type = ImageShapeType.Picture;
    }

    internal XmlImageShape(Shape shape)
    {
        Core = shape;
        Type = ImageShapeType.FilledShape;
    }

    public ImageShapeType Type { get; }

    public uint Id =>
        ((dynamic)Core).NonVisualPictureProperties?.NonVisualDrawingProperties?.Id?.Value ?? uint.MaxValue;

    public string Name => ((dynamic)Core).NonVisualPictureProperties?.NonVisualDrawingProperties?.Name ?? string.Empty;

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
}