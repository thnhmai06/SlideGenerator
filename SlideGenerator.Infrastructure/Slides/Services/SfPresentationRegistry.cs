using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Infrastructure.Slides.Adapters;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Manages open Syncfusion-backed presentations for workflow execution.
///     Write acquires are exclusive (one writer at a time); read acquires are shared.
/// </summary>
public sealed class SfPresentationRegistry : FileRegistry<IPresentation>
{
    /// <inheritdoc />
    protected override IPresentation CreateInstance(string normalizedPath, bool isEditable)
    {
        return new SfPresentation(normalizedPath, isEditable);
    }
}
