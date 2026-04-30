using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using SfPresNs = Syncfusion.Presentation;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Replaces image content in Syncfusion shapes.
///     Handles both <see cref="SfPresNs.IPicture" /> shapes and shapes with a picture fill (BlipFill equivalent).
/// </summary>
public sealed class SfImageComposer : IImageComposer
{
    /// <inheritdoc />
    public int Replace(IShape shape, Stream imageStream)
    {
        if (shape is not SfShape sfShape)
            throw new ArgumentException("Shape cannot be edited.", nameof(shape));
        if (!imageStream.CanRead)
            throw new ArgumentException("Image stream must be readable.", nameof(imageStream));

        if (imageStream.CanSeek)
            imageStream.Position = 0;

        using var ms = new MemoryStream();
        imageStream.CopyTo(ms);
        var imageBytes = ms.ToArray();

        if (sfShape.Core is SfPresNs.IPicture picture)
        {
            picture.ImageData = imageBytes;
            return 1;
        }

        if (sfShape.HasBlipFill)
        {
            sfShape.Core.Fill.PictureFill.ImageBytes = imageBytes;
            return 1;
        }

        return 0;
    }
}
