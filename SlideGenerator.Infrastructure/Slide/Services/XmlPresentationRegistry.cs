using SlideGenerator.Application.Resources;
using SlideGenerator.Domain.Slide.Entities.Presentation;
using SlideGenerator.Infrastructure.Slide.Adapters;

namespace SlideGenerator.Infrastructure.Slide.Services;

/// <summary>
///     Manages opened XML-based presentations for workflow execution.
/// </summary>
public sealed class XmlPresentationRegistry : Registry<IPresentation>
{
    /// <summary>
    ///     Opens an XML-based presentation from the normalized file path.
    /// </summary>
    /// <param name="normalizedPath">The normalized presentation file path.</param>
    /// <param name="isEditable">A value indicating whether the presentation should be opened for editing.</param>
    /// <returns>A new presentation adapter instance.</returns>
    protected override IPresentation OpenResource(string normalizedPath, bool isEditable)
    {
        return new XmlPresentation(normalizedPath, isEditable);
    }
}