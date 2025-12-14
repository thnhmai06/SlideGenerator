using SlideGenerator.Domain.Slide.Interfaces;
using CoreDerivedPresentation = SlideGenerator.Framework.Slide.Models.DerivedPresentation;

namespace SlideGenerator.Infrastructure.Adapters.Slide;

/// <summary>
///     Adapter to convert SlideGenerator.Framework.Slide.Models.DerivedPresentation to
///     Domain.Slide.Interfaces.IDerivedPresentation.
/// </summary>
internal sealed class DerivedPresentationAdapter(CoreDerivedPresentation presentation) : IDerivedPresentation
{
    public string FilePath => presentation.FilePath;

    public void AddSlideFromTemplate(Dictionary<string, string?> rowData)
    {
        // This is handled by SlideGenerator service directly
        throw new NotSupportedException("Use SlideGenerator service for adding slides from template.");
    }

    public void Save()
    {
        presentation.Save();
    }

    public void Dispose()
    {
        presentation.Dispose();
        GC.SuppressFinalize(this);
    }
}