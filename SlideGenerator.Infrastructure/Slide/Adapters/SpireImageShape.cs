using System.Drawing;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Interfaces;
using SlideGenerator.Domain.Slide.Models;
using SlideGenerator.Domain.Slide.Rules;
using Spire.Presentation;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class SpireImageShape : IImageShape, IPreviewable<ShapePreview>
{
    internal IShape Core { get; }
    
    internal SpireImageShape(SlidePicture core)
    {
        Core = core;
        Type = ImageShapeType.Picture;
    }
    
    internal SpireImageShape(Shape core)
    {
        Core = core;
        Type = ImageShapeType.FilledShape;
    }   

    public uint Id => Core.Id;
    public string Name => Core.Name;
    public required ImageShapeType Type { get; init; }
    public RectangleF Bounds => new(Core.Left, Core.Top, Core.Width, Core.Height);

    public ShapePreview GetPreview()
    {
        using var ms = new MemoryStream();
        using var img = Core.SaveAsImage();
        img.CopyTo(ms);
        return new ShapePreview(Id, Name, Bounds, ms.ToArray());
    }
}