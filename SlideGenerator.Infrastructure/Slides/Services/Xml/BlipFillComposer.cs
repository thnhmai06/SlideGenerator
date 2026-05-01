/* LEGACY-OPENXML — replaced by SfImageComposer (Syncfusion.Presentation.NET)
using System.Diagnostics.CodeAnalysis;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slides.Services;

public sealed class BlipFillComposer : IImageComposer
{
    public byte[]? Scan(IReadOnlyShape shape)
    {
        if (!shape.HasBlipFill || shape is not XmlShape { Slide: XmlSlide xmlSlide, Core: Shape xmlShape })
            return null;

        var slidePart = xmlSlide.Core;
        var relationshipId = xmlShape.ShapeProperties?.GetFirstChild<BlipFill>()?.Blip?.Embed?.Value;
        return TryReadPartBytes(slidePart, relationshipId, out var imageBytes) ? imageBytes : null;
    }

    public int Replace(IShape shape, Stream imageStream)
    {
        if (shape is not XmlShape xmlShape)
            throw new ArgumentException("Shape cannot be edited.", nameof(shape));
        if (xmlShape.Slide is not XmlSlide xmlSlide)
            throw new ArgumentException("Shape is not supported.", nameof(shape));
        if (!imageStream.CanRead)
            throw new ArgumentException("Image stream must be readable.", nameof(imageStream));
        if (!shape.HasBlipFill || xmlShape.Core is not Shape coreXmlShape)
            return 0;

        var blipFill = coreXmlShape.ShapeProperties?.GetFirstChild<BlipFill>();
        var embed = blipFill?.Blip?.Embed;
        if (embed == null)
            return 0;

        if (imageStream.CanSeek)
            imageStream.Position = 0;

        var slidePart = xmlSlide.Core;
        var imagePart = slidePart.AddImagePart(ImagePartType.Png);
        imagePart.FeedData(imageStream);
        embed.Value = slidePart.GetIdOfPart(imagePart);
        return 1;
    }

    private static bool TryReadPartBytes(SlidePart slidePart, string? relationshipId,
        [MaybeNullWhen(false)] out byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(relationshipId)
            || slidePart.GetPartById(relationshipId) is not ImagePart imagePart)
        {
            imageBytes = null;
            return false;
        }

        using var source = imagePart.GetStream(FileMode.Open, FileAccess.Read);
        using var target = new MemoryStream();
        source.CopyTo(target);
        imageBytes = target.ToArray();
        return true;
    }
}
*/
