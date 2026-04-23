using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Infrastructure.Slides.Adapters;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Manages open XML-based presentations for workflow execution.
/// </summary>
/// <remarks>
///     Each presentation file is accessed exclusively (max-count = 1) so that concurrent
///     slide-editing activities are serialized per output file.
/// </remarks>
/// <param name="locker">The keyed locker used to synchronize access to presentation files.</param>
public sealed class XmlPresentationRegistry(IAsyncKeyedLocker<string> locker)
    : FileRegistry<IPresentation>(locker)
{
    /// <summary>
    ///     Opens a presentation resource from the specified path.
    /// </summary>
    /// <param name="normalizedPath">The normalized file path.</param>
    /// <param name="isEditable">Indicates whether the presentation should be opened in editable mode.</param>
    /// <returns>An instance of <see cref="IPresentation" />.</returns>
    protected override IPresentation OpenResource(string normalizedPath, bool isEditable)
    {
        return new XmlPresentation(normalizedPath, isEditable);
    }
}