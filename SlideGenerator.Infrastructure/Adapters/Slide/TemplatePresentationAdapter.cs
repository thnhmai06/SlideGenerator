using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;

namespace SlideGenerator.Infrastructure.Adapters.Slide;

/// <summary>
///     Adapter to convert SlideGenerator.Framework.Slide.Models.TemplatePresentation to
///     Domain.Slide.Interfaces.ITemplatePresentation.
/// </summary>
internal sealed class TemplatePresentationAdapter(CoreTemplatePresentation presentation) : ITemplatePresentation
{
    public string FilePath => presentation.FilePath;

    public Dictionary<uint, ShapeImageData> GetAllImageShapes()
    {
        var coreShapes = presentation.GetAllPreviewImageShapes();
        return coreShapes.ToDictionary(
            kv => kv.Key,
            kv => new ShapeImageData(kv.Value.Name, kv.Value.ImageBytes));
    }

    public void Dispose()
    {
        presentation.Dispose();
        GC.SuppressFinalize(this);
    }
}