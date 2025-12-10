using DocumentFormat.OpenXml.Drawing;

namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when a presentation has no presentation part.
/// </summary>
public class NoPresentationPartException(string filepath)
    : ArgumentNullException($"The file '{filepath}' has no presentation part.")
{
    public string Filepath { get; } = filepath;
}

/// <summary>
/// Exception thrown when a slide has no relationship ID.
/// </summary>
public class NoRelationshipIdSlideException(string filepath, int pos)
    : ArgumentNullException($"The file '{filepath}' has no relationship ID for slide {pos}.")
{
    public string Filepath { get; } = filepath;
    public int Pos { get; } = pos;
}

/// <summary>
/// Exception thrown when a presentation has no slide ID list.
/// </summary>
public class NoSlideIdListException(string filepath)
    : ArgumentNullException($"The file '{filepath}' has no Slide ID List.")
{
    public string Filepath { get; } = filepath;
}

/// <summary>
/// Exception thrown when a presentation does not have exactly one slide.
/// </summary>
public class NotOnlySlidePresentationException(string filepath, int amount)
    : ArgumentException($"The file '{filepath}' is not a presentation with only slides. (Has {amount} slides)")
{
    public string Filepath { get; } = filepath;
    public int Amount { get; } = amount;
}

/// <summary>
/// Exception thrown when a shape does not contain an image.
/// </summary>
public class NoImageInShapeException : InvalidOperationException
{
    public readonly string ShapeId;

    private static string GetShapeId(Picture shape)
    {
        var nvPicPr = shape.NonVisualPictureProperties;
        return nvPicPr?.NonVisualDrawingProperties?.Id != null
            ? nvPicPr.NonVisualDrawingProperties.Id.Value.ToString()
            : "Unknown";
    }

    private static string GetShapeId(Shape shape)
    {
        var nvShapePr = shape.NonVisualShapeProperties;
        var nvPicPr = nvShapePr?.NonVisualDrawingProperties;
        return nvPicPr?.Id != null ? nvPicPr.Id.Value.ToString() : "Unknown";
    }

    public NoImageInShapeException(Picture shape, bool onEmbed) : base(
        $"The provided shape (ID: {GetShapeId(shape)}) does not contain {(onEmbed ? "Embed on" : "")} Blip (Image) element.")
    {
        ShapeId = GetShapeId(shape);
    }

    public NoImageInShapeException(Shape shape, bool onEmbed) : base(
        $"The provided shape (ID: {GetShapeId(shape)}) does not {(onEmbed ? "contain Embed on" : "filled by")} Blip (Image) element.")
    {
        ShapeId = GetShapeId(shape);
    }
}
