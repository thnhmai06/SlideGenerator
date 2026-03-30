using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Infrastructure.Slide.Adapters;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using BlipFill = DocumentFormat.OpenXml.Drawing.BlipFill;

namespace SlideGenerator.Infrastructure.Slide.Services;

/// <summary>
///     OpenXml-based implementation for replacing slide content (text and images).
/// </summary>
public sealed class XmlSlideContentOperator : ISlideContentOperator
{
    public (IReadOnlySet<string> Placeholders, IReadOnlySet<uint> ImageShapeIds) ScanTemplateContent(ISlide slide)
    {
        if (slide is not XmlSlide xmlSlide || xmlSlide.Core.Slide == null)
            return (new HashSet<string>(StringComparer.Ordinal), new HashSet<uint>());

        var placeholders = xmlSlide.Core.ScanMustache()
            .Select(x => x.Mustache)
            .ToHashSet(StringComparer.Ordinal);

        var imageShapeIds = xmlSlide.EnumerateImageShapes()
            .Select(shape => shape.Id)
            .ToHashSet();

        return (placeholders, imageShapeIds);
    }

    /// <summary>
    ///     Replaces text placeholders on the specified slide.
    /// </summary>
    public int ReplaceText(ISlide slide, IReadOnlyDictionary<string, string> replacements)
    {
        if (replacements.Count == 0)
            return 0;

        if (slide is not XmlSlide xmlSlide)
            return 0;

        var changes = xmlSlide.Core.RenderMustacheAsync(replacements).GetAwaiter().GetResult();
        return changes.Count;
    }

    /// <summary>
    ///     Replaces image contents on the specified slide by shape identifier.
    /// </summary>
    public int ReplaceImages(ISlide slide, IReadOnlyDictionary<uint, string> assignments)
    {
        if (assignments.Count == 0)
            return 0;

        if (slide is not XmlSlide xmlSlide || xmlSlide.Core.Slide == null)
            return 0;

        var replaced = 0;

        foreach (var picture in xmlSlide.Core.Slide.Descendants<Picture>())
        {
            var shapeId = picture.NonVisualPictureProperties?.NonVisualDrawingProperties?.Id?.Value;
            if (!shapeId.HasValue)
                continue;
            if (!assignments.TryGetValue(shapeId.Value, out var imagePath) || !File.Exists(imagePath))
                continue;

            using var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (xmlSlide.Core.ReplaceImage(picture, fs))
                replaced++;
        }

        foreach (var shape in xmlSlide.Core.Slide.Descendants<Shape>())
        {
            var shapeId = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value;
            var hasImageFill = shape.ShapeProperties?.GetFirstChild<BlipFill>()?.Blip?.Embed != null;
            if (!shapeId.HasValue || !hasImageFill)
                continue;
            if (!assignments.TryGetValue(shapeId.Value, out var imagePath) || !File.Exists(imagePath))
                continue;

            using var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (xmlSlide.Core.ReplaceImage(shape, fs))
                replaced++;
        }

        return replaced;
    }
}
