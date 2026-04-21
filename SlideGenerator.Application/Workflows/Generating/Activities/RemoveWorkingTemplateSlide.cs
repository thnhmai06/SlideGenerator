using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Removes a slide from presentation by 1-based index.
/// </summary>
public sealed class RemoveWorkingTemplateSlide(FileRegistry<IPresentation> slideRegistry) : Activity
{
    /// <summary>Input working template slide identifier.</summary>
    public required Input<SlideIdentifier> WorkingTemplateSlide { get; init; }

    /// <summary>Output whether a slide was removed.</summary>
    public Output<bool> Removed { get; init; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideIdentifier = context.Get(WorkingTemplateSlide);
        if (slideIdentifier is null)
            throw new InvalidOperationException("Template slide identifier is not provided.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, context.CancellationToken)
            .ConfigureAwait(false);

        context.Set(Removed, lease.Value.RemoveSlide(slideIdentifier.Index));
    }
}
