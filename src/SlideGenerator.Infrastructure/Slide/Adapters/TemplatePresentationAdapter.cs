using DocumentFormat.OpenXml.Drawing;
using SlideGenerator.Domain.Slide;
using SlideGenerator.Domain.Slide.Components;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;
using Presentation = SlideGenerator.Framework.Slide.Models.Presentation;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Picture = DocumentFormat.OpenXml.Drawing.Picture;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

/// <summary>
///     Adapter to convert SlideGenerator.Framework.Slide.Models.TemplatePresentation to
///     Domain.Slide.Interfaces.ITemplatePresentation.
/// </summary>
internal sealed class TemplatePresentationAdapter(CoreTemplatePresentation presentation)
    : ITemplatePresentation, IDisposable
{
    public void Dispose()
    {
        presentation.Dispose();
    }

    public string FilePath => presentation.FilePath;

    public int SlideCount => presentation.SlideCount;

    public Dictionary<uint, ImagePreview> GetAllImageShapes()
    {
        var coreShapes = presentation.GetAllPreviewImageShapes();
        return coreShapes.ToDictionary(
            kv => kv.Key,
            kv => new ImagePreview(kv.Value.Name, kv.Value.ImageBytes));
    }

    public IReadOnlyList<ShapeInfo> GetAllShapes()
    {
        var slidePart = presentation.GetSlidePart();
        if (slidePart == null) return [];

        var shapes = new List<ShapeInfo>();
        foreach (var shape in Presentation.GetShapes(slidePart))
        {
            var id = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value ?? 0;
            var name = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value ?? string.Empty;
            shapes.Add(new ShapeInfo(id, name, nameof(Shape), HasImageFill(shape)));
        }

        foreach (var picture in Presentation.GetPictures(slidePart))
        {
            var id = picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Id?.Value ?? 0;
            var name = picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value ?? string.Empty;
            shapes.Add(new ShapeInfo(id, name, nameof(Picture), true));
        }

        return shapes;
    }

    private static bool HasImageFill(Shape shape)
    {
        var shapeProps = shape.ShapeProperties;
        if (shapeProps == null) return false;

        return shapeProps.GetFirstChild<BlipFill>() != null;
    }
}