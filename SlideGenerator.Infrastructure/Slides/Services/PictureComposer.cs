using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Replaces picture images in Open XML shapes.
/// </summary>
public sealed class PictureComposer : IImageComposer
{
    /// <summary>
    ///     Scans a shape for picture image data.
    /// </summary>
    /// <param name="sample">The shape to scan.</param>
    /// <returns>The image data as a byte array if found; otherwise, an empty array.</returns>
    public byte[]? Scan(IReadOnlyShape shape)
    {
        if (!shape.IsPicture || shape is not XmlShape { Slide: XmlSlide xmlSlide, Core: Picture picture })
            return [];

        var slidePart = xmlSlide.Core;
        var relationshipId = picture.BlipFill?.Blip?.Embed?.Value;
        return TryReadPartBytes(slidePart, relationshipId, out var imageBytes) ? imageBytes : [];
    }

    /// <summary>
    ///     Replaces the picture image in a shape with data from a stream.
    /// </summary>
    /// <param name="sample">The shape to update.</param>
    /// <param name="imageStream">The stream containing the new image data.</param>
    /// <returns>The number of shapes updated (1 if successful, 0 otherwise).</returns>
    /// <exception cref="ArgumentException">Thrown when the shape or stream is invalid.</exception>
    public int Replace(IShape shape, Stream imageStream)
    {
        if (shape is not XmlShape xmlShape)
            throw new ArgumentException("Shape cannot be edited.", nameof(shape));
        if (xmlShape.Slide is not XmlSlide xmlSlide)
            throw new ArgumentException("Shape is not supported.", nameof(shape));
        if (!imageStream.CanRead)
            throw new ArgumentException("Image stream must be readable.", nameof(imageStream));
        if (!shape.IsPicture || xmlShape.Core is not Picture picture)
            return 0;

        if (imageStream.CanSeek)
            imageStream.Position = 0;

        var blip = picture.Descendants<Blip>().FirstOrDefault();
        var embed = blip?.Embed;
        if (embed == null)
            return 0;

        var slidePart = xmlSlide.Core;
        var imagePart = slidePart.AddImagePart(ImagePartType.Png);
        imagePart.FeedData(imageStream);
        embed.Value = slidePart.GetIdOfPart(imagePart);
        return 1;
    }

    /// <summary>
    ///     Attempts to read the bytes of an image part from a slide part using a relationship ID.
    /// </summary>
    /// <param name="slidePart">The slide part containing the image.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="imageBytes">The resulting image data as a byte array.</param>
    /// <returns><see langword="true" /> if the image data was successfully read; otherwise, <see langword="false" />.</returns>
    private static bool TryReadPartBytes(SlidePart slidePart, string? relationshipId, out byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(relationshipId)
            || slidePart.GetPartById(relationshipId) is not ImagePart imagePart)
        {
            imageBytes = [];
            return false;
        }

        using var source = imagePart.GetStream(FileMode.Open, FileAccess.Read);
        using var target = new MemoryStream();
        source.CopyTo(target);
        imageBytes = target.ToArray();
        return true;
    }
}