using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;

namespace SlideGenerator.Infrastructure.Slides.Services;

public sealed class PictureReplacer : IImageReplacer
{
    public byte[]? Scan(IReadOnlyShape sample)
    {
        if (!sample.IsPicture || sample is not XmlShape { Slide: XmlSlide xmlSlide, Core: Picture picture })
            return [];

        var slidePart = xmlSlide.Core;
        var relationshipId = picture.BlipFill?.Blip?.Embed?.Value;
        return TryReadPartBytes(slidePart, relationshipId, out var imageBytes) ? imageBytes : [];
    }

    public int Replace(IShape sample, Stream imageStream)
    {
        if (sample is not XmlShape xmlShape)
            throw new ArgumentException("Shape cannot be edited.", nameof(sample));
        if (xmlShape.Slide is not XmlSlide xmlSlide)
            throw new ArgumentException("Shape is not supported.", nameof(sample));
        if (!imageStream.CanRead)
            throw new ArgumentException("Image stream must be readable.", nameof(imageStream));
        if (!sample.IsPicture || xmlShape.Core is not Picture picture)
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