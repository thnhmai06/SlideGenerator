using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Aggregates multiple image composers to provide a unified scan and replace mechanism.
/// </summary>
/// <param name="pictureComposer">The composer specialized for picture shapes.</param>
/// <param name="blipFillComposer">The composer specialized for blip fill shapes.</param>
public sealed class ImageComposer(
    PictureComposer pictureComposer,
    BlipFillComposer blipFillComposer) : IImageComposer
{
    /// <inheritdoc />
    public byte[]? Scan(IReadOnlyShape shape)
    {
        // Prioritize Picture over Blip as requested
        if (shape.IsPicture)
        {
            return pictureComposer.Scan(shape);
        }

        if (shape.HasBlipFill)
        {
            return blipFillComposer.Scan(shape);
        }

        return null;
    }

    /// <inheritdoc />
    public int Replace(IShape shape, Stream imageStream)
    {
        // Prioritize Picture over Blip as requested
        if (shape.IsPicture)
        {
            return pictureComposer.Replace(shape, imageStream);
        }

        if (shape.HasBlipFill)
        {
            return blipFillComposer.Replace(shape, imageStream);
        }

        return 0;
    }
}
