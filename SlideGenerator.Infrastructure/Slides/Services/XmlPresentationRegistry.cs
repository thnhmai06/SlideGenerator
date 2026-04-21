using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Infrastructure.Slides.Adapters;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Manages open XML-based presentations for workflow execution.
///     Each presentation file is accessed exclusively (max-count = 1) so that concurrent
///     slide-editing activities are serialised per output file.
/// </summary>
public sealed class XmlPresentationRegistry(IAsyncKeyedLocker<string> locker)
    : FileRegistry<IPresentation>(locker)
{
    /// <inheritdoc />
    protected override IPresentation OpenResource(string normalizedPath, bool isEditable)
        => new XmlPresentation(normalizedPath, isEditable);
}
