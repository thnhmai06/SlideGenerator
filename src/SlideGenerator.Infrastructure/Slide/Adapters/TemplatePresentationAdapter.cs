using SlideGenerator.Domain.Slide;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Framework.Slide;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;
using Picture = DocumentFormat.OpenXml.Drawing.Picture;
using Presentation = SlideGenerator.Framework.Slide.Models.Presentation;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

/// <summary>
///     Adapter to convert SlideGenerator.Framework.Slide.Models.TemplatePresentation to
///     Domain.Slide.Interfaces.ITemplatePresentation.
/// </summary>
internal sealed class TemplatePresentationAdapter(CoreTemplatePresentation presentation)
    : ITemplatePresentation
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

        var previews = presentation.GetAllPreviewImageShapes();
        var shapes = new List<ShapeInfo>(previews.Count);

        foreach (var (id, preview) in previews)
        {
            var picture = Presentation.GetPictureById(slidePart, id);
            if (picture != null)
            {
                var name = picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value
                           ?? preview.Name;
                shapes.Add(new ShapeInfo(id, name, nameof(Picture), true));
                continue;
            }

            var shape = Presentation.GetShapeById(slidePart, id);
            var shapeName = shape?.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value
                            ?? preview.Name;
            shapes.Add(new ShapeInfo(id, shapeName, nameof(Shape), true));
        }

        return shapes;
    }

    public IReadOnlyCollection<string> GetAllTextPlaceholders()
    {
        var slidePart = presentation.GetSlidePart();
        if (slidePart == null) return Array.Empty<string>();

        return TextReplacer.ScanPlaceholders(slidePart)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}