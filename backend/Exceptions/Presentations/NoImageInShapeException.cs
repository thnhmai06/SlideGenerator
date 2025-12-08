using DocumentFormat.OpenXml.Drawing;

namespace TaoSlideTotNghiep.Exceptions.Presentations;

public class NoImageInShapeException : InvalidOperationException
{
    public readonly string ShapeId;

    private static string GetShapeId(Picture shape)
    {
        var nvPicPr = shape.NonVisualPictureProperties;
        return nvPicPr?.NonVisualDrawingProperties?.Id != null ? nvPicPr.NonVisualDrawingProperties.Id.Value.ToString() : "Unknown";
    }

    private static string GetShapeId(Shape shape)
    {
        var nvShapePr = shape.NonVisualShapeProperties;
        var nvPicPr = nvShapePr?.NonVisualDrawingProperties;
        return nvPicPr?.Id != null ? nvPicPr.Id.Value.ToString() : "Unknown";
    }

    public NoImageInShapeException(Picture shape, bool onEmbed) : base($"The provided shape (ID: {GetShapeId(shape)}) does not contain {(onEmbed ? "Embed on" : "")} Blip (Image) element.")
    {
        ShapeId = GetShapeId(shape);
    }

    public NoImageInShapeException(Shape shape, bool onEmbed) : base($"The provided shape (ID: {GetShapeId(shape)}) does not {(onEmbed ? "contain Embed on" : "filled by")} Blip (Image) element.")
    {
        ShapeId = GetShapeId(shape);
    }
}
