using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Infrastructure.Slides.Adapters;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Manages open Syncfusion-backed presentations for workflow execution.
///     Write acquires are exclusive (one writer at a time); read acquires are shared.
/// </summary>
/// <param name="locker">The reader-writer locker used to synchronise access to presentation files.</param>
public sealed class SfPresentationRegistry(FileLocker locker)
    : FileRegistry<IPresentation>(locker)
{
    /// <inheritdoc />
    protected override IPresentation CreateInstance(string normalizedPath, bool isEditable)
    {
        return new SfPresentation(normalizedPath, isEditable);
    }
}
