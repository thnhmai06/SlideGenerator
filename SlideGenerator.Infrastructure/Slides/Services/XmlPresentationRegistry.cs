using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Infrastructure.Slides.Adapters;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Manages open XML-based presentations for workflow execution.
///     Write acquires are exclusive (one writer at a time); read acquires are shared.
/// </summary>
/// <param name="locker">The reader-writer locker used to synchronize access to presentation files.</param>
public sealed class XmlPresentationRegistry(FileLocker locker)
    : FileRegistry<IPresentation>(locker)
{
    /// <summary>
    ///     Opens a presentation resource from the specified path.
    /// </summary>
    /// <param name="normalizedPath">The normalized file path.</param>
    /// <param name="isEditable">Indicates whether the presentation should be opened in editable mode.</param>
    /// <returns>An instance of <see cref="IPresentation" />.</returns>
    protected override IPresentation CreateInstance(string normalizedPath, bool isEditable)
    {
        return new XmlPresentation(normalizedPath, isEditable);
    }
}