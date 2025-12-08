using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using TaoSlideTotNghiep.Exceptions.Presentations;

namespace TaoSlideTotNghiep.Models.Engines;

public static class ImageReplacementEngine
{
    public static void ReplaceImage(SlidePart slidePart, Picture shape, Stream pngStream)
    {
        var imgPart = slidePart.AddImagePart(ImagePartType.Png);
        imgPart.FeedData(pngStream);
        var rId = slidePart.GetIdOfPart(imgPart);

        var blip = shape.Descendants<Blip>()?.First();
        if (blip is null) throw new NoImageInShapeException(shape, false);
        var embed = blip.Embed;
        if (embed is null) throw new NoImageInShapeException(shape, true);
        embed.Value = rId;

        slidePart.Slide.Save();
    }

    public static void ReplaceImage(SlidePart slidePart, Shape shape, Stream pngStream)
    {
        var imgPart = slidePart.AddImagePart(ImagePartType.Png);
        imgPart.FeedData(pngStream);
        var rId = slidePart.GetIdOfPart(imgPart);

        var blip = shape.Descendants<BlipFill>()?.First()?.Blip;
        if (blip is null) throw new NoImageInShapeException(shape, false);
        var embed = blip.Embed;
        if (embed is null) throw new NoImageInShapeException(shape, true);
        embed.Value = rId;

        slidePart.Slide.Save();
    }
}
