using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;

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

    public Dictionary<uint, ShapeImagePreview> GetAllImageShapes()
    {
        var coreShapes = presentation.GetAllPreviewImageShapes();
        return coreShapes.ToDictionary(
            kv => kv.Key,
            kv => new ShapeImagePreview(kv.Value.Name, kv.Value.ImageBytes));
    }
}