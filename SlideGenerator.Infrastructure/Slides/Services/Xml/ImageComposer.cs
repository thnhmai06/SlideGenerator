/* LEGACY-OPENXML — replaced by SfImageComposer (Syncfusion.Presentation.NET)
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Infrastructure.Slides.Services;

public sealed class ImageComposer(
    PictureComposer pictureComposer,
    BlipFillComposer blipFillComposer) : IImageComposer
{
    public byte[]? Scan(IReadOnlyShape shape)
    {
        if (shape.IsPicture) return pictureComposer.Scan(shape);
        if (shape.HasBlipFill) return blipFillComposer.Scan(shape);
        return null;
    }

    public int Replace(IShape shape, Stream imageStream)
    {
        if (shape.IsPicture) return pictureComposer.Replace(shape, imageStream);
        if (shape.HasBlipFill) return blipFillComposer.Replace(shape, imageStream);
        return 0;
    }
}
*/
