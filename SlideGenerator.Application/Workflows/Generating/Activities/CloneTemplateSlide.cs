using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Clones a source slide inside the presentation and inserts it at a target 1-based position.
/// </summary>
public sealed class CloneTemplateSlide(FileRegistry<IPresentation> slideRegistry) : Activity
{
    /// <summary>Path to the presentation file where the slide should be cloned.</summary>
    public required Input<SlideIdentifier> TemplateSlide { get; init; }

    /// <summary>1-based index where a cloned slide should be inserted.</summary>
    public required Input<int> InsertAtIndex { get; init; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var templateSlideIdentifier = context.Get(TemplateSlide);
        var insertAtIndex = context.Get(InsertAtIndex);
        if (templateSlideIdentifier is null || insertAtIndex < 1)
            throw new InvalidOperationException("Template slide identifier and insert index must be valid.");

        using var lease = await slideRegistry
            .AcquireAsync(templateSlideIdentifier.Presentation.FilePath, true, context.CancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
        if (presentation.EnumerateSlides().ElementAtOrDefault(templateSlideIdentifier.Index - 1) == null)
            throw new InvalidOperationException(
                $"Cannot clone slide {templateSlideIdentifier.Index}: source slide does not exist.");

        _ = presentation.CopySlide(templateSlideIdentifier.Index, insertAtIndex);
    }
}
