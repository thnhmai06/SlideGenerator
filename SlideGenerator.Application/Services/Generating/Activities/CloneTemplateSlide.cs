using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>Clones a slide within a presentation.</summary>
/// <remarks>Inserts the copy at a 1-based position calculated from the template index and current row.</remarks>
/// <param name="slideRegistry">The presentation file registry.</param>
/// <param name="templateSlideIndex">The 1-based index of the template slide.</param>
public sealed class CloneTemplateSlide(FileRegistry<IPresentation> slideRegistry, int templateSlideIndex) : Activity
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if the template slide is missing or the insert index is invalid.</exception>
    public override async ValueTask ExecuteAsync(IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var templateSlideIdentifier = context.GetVariable<SlideIdentifier>(WorksheetContextRules.WorkingTemplateSlide)
                                      ?? throw new InvalidOperationException("Template slide must be set in context.");
        var row = context.GetVariable<int>(WorksheetContextRules.Row);
        var insertAtIndex = templateSlideIndex + row;
        if (insertAtIndex < 1)
            throw new InvalidOperationException("Insert index must be >= 1.");

        using var lease = await slideRegistry
            .AcquireAsync(templateSlideIdentifier.Presentation.FilePath, true, cancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
        if (presentation.EnumerateSlides().ElementAtOrDefault(templateSlideIdentifier.Index - 1) == null)
            throw new InvalidOperationException(
                $"Cannot clone slide {templateSlideIdentifier.Index}: source slide does not exist.");

        _ = presentation.CopySlide(templateSlideIdentifier.Index, insertAtIndex);
    }
}