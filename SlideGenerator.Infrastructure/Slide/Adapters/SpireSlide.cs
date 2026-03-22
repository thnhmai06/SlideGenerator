using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Interfaces;
using SlideGenerator.Domain.Slide.Models;
using SlideGenerator.Domain.Slide.Rules;
using Spire.Presentation;
using Spire.Presentation.Drawing;
using ISlide = SlideGenerator.Domain.Slide.Entities.ISlide;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class SpireSlide : ISlide, IPreviewable<SlidePreview>
{
    internal Spire.Presentation.ISlide Core { get; }

    internal SpireSlide(Spire.Presentation.ISlide core)
    {
        Core = core;
    }

    public uint Id => Core.SlideID;
    
    public string Name => Core.Name;
    
    public required int Index { get; init; }

    public IEnumerable<IImageShape> EnumerateImageShapes()
    {
        foreach (var shape in Core.Shapes.ToArray())
        {
            if (shape.IsHidden) continue;
            switch (shape)
            {
                case SlidePicture picture:
                    yield return new SpireImageShape(picture) { Type = ImageShapeType.Picture };
                    break;
                case Shape filledShape when shape.Fill.FillType == FillFormatType.Picture:
                    yield return new SpireImageShape(filledShape) { Type = ImageShapeType.FilledShape };
                    break;
            }
        }
    }

    public SlidePreview GetPreview()
    {
        using var ms = new MemoryStream();
        using var img = Core.SaveAsImage();
        img.CopyTo(ms);
        return new SlidePreview(Index, Id, Name, ms.ToArray());
    }
}